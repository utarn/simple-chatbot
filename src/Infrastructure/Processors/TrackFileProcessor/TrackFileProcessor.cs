using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Application.Common.Models;
using ChatbotApi.Application.Common.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Net.Http;
using ChatbotApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Pgvector.EntityFrameworkCore;

namespace ChatbotApi.Infrastructure.Processors.TrackFileProcessor
{
    public class TrackFileProcessor : ILineMessageProcessor
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ISystemService _systemService;
        private readonly ILogger<TrackFileProcessor> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IApplicationDbContext _context;
        private readonly IOpenAiService _openAiService;

        private const string SessionKeyPrefix = "trackfile_";
        private const string ContactSessionKeyPrefix = "trackfile_contact_";
        private const string ModeKeyPrefix = "trackfile_mode_";
        
        public string Name => "TrackFile";
        public string Description => "จัดการไฟล์แนบ (Line)";

        public TrackFileProcessor(
            IMemoryCache memoryCache,
            ISystemService systemService,
            ILogger<TrackFileProcessor> logger,
            IWebHostEnvironment environment,
            IHttpClientFactory httpClientFactory,
            IApplicationDbContext context,
            IOpenAiService openAiService)
        {
            _memoryCache = memoryCache;
            _systemService = systemService;
            _logger = logger;
            _environment = environment;
            _httpClientFactory = httpClientFactory;
            _context = context;
            _openAiService = openAiService;
        }

        public async Task<LineReplyStatus> ProcessLineAsync(LineEvent evt, int chatbotId, string message, string userId,
            string replyToken, CancellationToken cancellationToken = default)
        {
            // Check if user is in a specific mode
            var modeKey = ModeKeyPrefix + userId;
            string currentMode = "";
            _memoryCache.TryGetValue(modeKey, out currentMode);

            // Handle CONTACT mode
            if (currentMode == "CONTACT")
            {
                return await ProcessContactModeAsync(message, userId, replyToken, cancellationToken);
            }
            
            // Handle GET mode
            if (currentMode == "GET")
            {
                return await ProcessGetModeAsync(message, chatbotId, userId, replyToken, cancellationToken);
            }

            // Check if user is in the middle of an upload session
            var sessionKey = SessionKeyPrefix + userId;
            if (_memoryCache.TryGetValue(sessionKey, out PendingUpload? pendingUpload))
            {
                // Check if user is in the middle of contact selection
                var contactSessionKey = ContactSessionKeyPrefix + userId;
                if (_memoryCache.TryGetValue(contactSessionKey, out ContactSelectionState? contactState))
                {
                    // User is expected to select a contact
                    return await ProcessContactSelectionAsync(message, userId, replyToken, contactState, pendingUpload, chatbotId, cancellationToken);
                }
                
                // User is expected to provide a description for the file
                var description = message?.Trim();
                if (string.IsNullOrWhiteSpace(description))
                {
                    return new LineReplyStatus
                    {
                        Status = 400,
                        ReplyMessage = new LineReplyMessage
                        {
                            ReplyToken = replyToken,
                            Messages = new List<LineMessage> { new LineTextMessage("กรุณาระบุคำอธิบายไฟล์") }
                        }
                    };
                }

                try
                {
                    // Get chatbot for LLM key
                    var chatbot =
                        await _context.Chatbots.FirstOrDefaultAsync(c => c.Id == chatbotId, cancellationToken);
                    if (chatbot?.LlmKey == null)
                    {
                        return new LineReplyStatus
                        {
                            Status = 500,
                            ReplyMessage = new LineReplyMessage
                            {
                                ReplyToken = replyToken,
                                Messages = new List<LineMessage>
                                {
                                    new LineTextMessage("ระบบยังไม่ได้ตั้งค่า API Key")
                                }
                            }
                        };
                    }

                    // Create filename with timestamp
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var extension = MimeTypes.GetMimeTypeExtensions(pendingUpload?.FileExtension ?? string.Empty).LastOrDefault() ??
                                    "bin";
                    var fileName = $"{timestamp}_{userId}_{description.MakeValidFileName()}.{extension}";

                    // Generate embedding for the description, filename, and file type information
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                    var fileExtension = Path.GetExtension(fileName).TrimStart('.');
                    
                    // Build embedding text with file type information
                    var embeddingTextParts = new List<string> { description, fileNameWithoutExtension };
                    
                    // Add file type information
                    if (pendingUpload?.FileExtension?.StartsWith("image/") == true)
                    {
                        embeddingTextParts.Add("image รูปภาพ");
                    }
                    
                    // Add file extension for all files
                    if (!string.IsNullOrEmpty(fileExtension))
                    {
                        embeddingTextParts.Add(fileExtension);
                    }
                    
                    var embeddingText = string.Join(" ", embeddingTextParts);
                    var embedding =
                        await _openAiService.CallEmbeddingsAsync(embeddingText, chatbot.LlmKey, cancellationToken);
                    if (embedding == null)
                    {
                        return new LineReplyStatus
                        {
                            Status = 500,
                            ReplyMessage = new LineReplyMessage
                            {
                                ReplyToken = replyToken,
                                Messages = new List<LineMessage>
                                {
                                    new LineTextMessage("ไม่สามารถประมวลผลคำอธิบายได้ กรุณาลองใหม่อีกครั้ง")
                                }
                            }
                        };
                    }

                    // Create file path with year directory
                    var year = DateTime.Now.Year.ToString();
                    var userDir = Path.Combine(_environment.ContentRootPath, "storage", userId, year);
                    if (!Directory.Exists(userDir))
                        Directory.CreateDirectory(userDir);
                    var filePath = Path.Combine(userDir, fileName);

                    // Save file to disk
                    await File.WriteAllBytesAsync(filePath, pendingUpload?.FileContent!, cancellationToken);

                    // Calculate file hash
                    var fileHash = CalculateFileHash(pendingUpload?.FileContent ?? Array.Empty<byte>());

                    // Save file information to database with embedding
                    var trackFile = new TrackFile
                    {
                        FileName = fileName,
                        FilePath = filePath,
                        ContentType = pendingUpload?.FileExtension ?? "application/octet-stream",
                        FileSize = pendingUpload?.FileContent?.Length ?? 0,
                        UserId = userId,
                        Description = description,
                        Embedding = embedding,
                        FileHash = fileHash,
                        ChatbotId = chatbotId,
                        ContactId = contactState?.SelectedContactId > 0 ? contactState.SelectedContactId : (int?)null
                    };

                    await _context.TrackFiles.AddAsync(trackFile, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);

                    _memoryCache.Remove(sessionKey);

                    var fileUrl = $"{_systemService.FullHostName}/information/getfile?id={trackFile.Id}";
                    return new LineReplyStatus
                    {
                        Status = 200,
                        ReplyMessage = new LineReplyMessage
                        {
                            ReplyToken = replyToken,
                            Messages = new List<LineMessage>
                            {
                                new LineTextMessage(
                                    $"บันทึกไฟล์ '{description}' เรียบร้อยแล้ว ดาวน์โหลด: {fileUrl}")
                            }
                        }
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save file for user {UserId}", userId);
                    return new LineReplyStatus
                    {
                        Status = 500,
                        ReplyMessage = new LineReplyMessage
                        {
                            ReplyToken = replyToken,
                            Messages = new List<LineMessage>
                            {
                                new LineTextMessage("ไม่สามารถบันทึกไฟล์ได้ กรุณาลองใหม่อีกครั้ง")
                            }
                        }
                    };
                }
            }

            // Handle mode switching commands
            if (message?.Trim().Equals("CONTACT", StringComparison.OrdinalIgnoreCase) == true)
            {
                _memoryCache.Set(modeKey, "CONTACT", TimeSpan.FromMinutes(10));
                return new LineReplyStatus
                {
                    Status = 200,
                    ReplyMessage = new LineReplyMessage
                    {
                        ReplyToken = replyToken,
                        Messages = new List<LineMessage> 
                        { 
                            new LineTextMessage("เข้าสู่โหมดจัดการผู้ติดต่อ\nพิมพ์ 'ADD [ชื่อ]' เพื่อเพิ่มผู้ติดต่อ\nพิมพ์ 'DELETE [ชื่อ]' เพื่อลบผู้ติดต่อ\nพิมพ์ 'LIST' เพื่อดูรายชื่อผู้ติดต่อ\nพิมพ์ 'EXIT' เพื่อออกจากโหมดนี้") 
                        }
                    }
                };
            }
            
            if (message?.Trim().Equals("GET", StringComparison.OrdinalIgnoreCase) == true)
            {
                _memoryCache.Set(modeKey, "GET", TimeSpan.FromMinutes(10));
                return new LineReplyStatus
                {
                    Status = 200,
                    ReplyMessage = new LineReplyMessage
                    {
                        ReplyToken = replyToken,
                        Messages = new List<LineMessage>
                        {
                            new LineTextMessage("เข้าสู่โหมดค้นหาไฟล์\nพิมพ์คำค้นหาเพื่อค้นหาไฟล์\nพิมพ์ 'EXIT' เพื่อออกจากโหมดนี้")
                        }
                    }
                };
            }

            // When app is not in any mode and user types text, provide brief instructions about available modes
            if (!string.IsNullOrWhiteSpace(message))
            {
                return new LineReplyStatus
                {
                    Status = 200,
                    ReplyMessage = new LineReplyMessage
                    {
                        ReplyToken = replyToken,
                        Messages = new List<LineMessage>
                        {
                            new LineTextMessage("ระบบจัดการไฟล์\n- พิมพ์ 'CONTACT' เพื่อจัดการผู้ติดต่อ\n- พิมพ์ 'GET' เพื่อค้นหาไฟล์\n- หรือส่งไฟล์เพื่ออัปโหลด")
                        }
                    }
                };
            }

            // Default: ask user to upload a file
            return new LineReplyStatus
            {
                Status = 200,
                ReplyMessage = new LineReplyMessage
                {
                    ReplyToken = replyToken,
                    Messages = new List<LineMessage> { new LineTextMessage("กรุณาส่งไฟล์ที่ต้องการอัปโหลด") }
                }
            };
        }

        private async Task<LineReplyStatus> ProcessContactModeAsync(string message, string userId, string replyToken, CancellationToken cancellationToken)
        {
            var modeKey = ModeKeyPrefix + userId;
            var trimmedMessage = message?.Trim() ?? "";

            // Handle EXIT command
            if (trimmedMessage.Equals("EXIT", StringComparison.OrdinalIgnoreCase))
            {
                _memoryCache.Remove(modeKey);
                return new LineReplyStatus
                {
                    Status = 200,
                    ReplyMessage = new LineReplyMessage
                    {
                        ReplyToken = replyToken,
                        Messages = new List<LineMessage> { new LineTextMessage("ออกจากโหมดจัดการผู้ติดต่อ") }
                    }
                };
            }

            // Handle ADD command
            if (trimmedMessage.StartsWith("ADD ", StringComparison.OrdinalIgnoreCase))
            {
                var contactName = trimmedMessage.Substring(4).Trim();
                if (!string.IsNullOrWhiteSpace(contactName))
                {
                    // Check if contact already exists
                    var existingContact = await _context.Contacts
                        .FirstOrDefaultAsync(c => c.UserId == userId && c.Name == contactName, cancellationToken);
                    
                    if (existingContact != null)
                    {
                        return new LineReplyStatus
                        {
                            Status = 200,
                            ReplyMessage = new LineReplyMessage
                            {
                                ReplyToken = replyToken,
                                Messages = new List<LineMessage> { new LineTextMessage($"ผู้ติดต่อ '{contactName}' มีอยู่แล้ว") }
                            }
                        };
                    }

                    // Add new contact
                    var newContact = new Contact
                    {
                        Name = contactName,
                        UserId = userId
                    };

                    await _context.Contacts.AddAsync(newContact, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);

                    return new LineReplyStatus
                    {
                        Status = 200,
                        ReplyMessage = new LineReplyMessage
                        {
                            ReplyToken = replyToken,
                            Messages = new List<LineMessage> { new LineTextMessage($"เพิ่มผู้ติดต่อ '{contactName}' เรียบร้อยแล้ว") }
                        }
                    };
                }
                else
                {
                    return new LineReplyStatus
                    {
                        Status = 400,
                        ReplyMessage = new LineReplyMessage
                        {
                            ReplyToken = replyToken,
                            Messages = new List<LineMessage> { new LineTextMessage("กรุณาระบุชื่อผู้ติดต่อ") }
                        }
                    };
                }
            }

            // Handle DELETE command
            if (trimmedMessage.StartsWith("DELETE ", StringComparison.OrdinalIgnoreCase))
            {
                var contactName = trimmedMessage.Substring(7).Trim();
                if (!string.IsNullOrWhiteSpace(contactName))
                {
                    // Find contact
                    var contact = await _context.Contacts
                        .FirstOrDefaultAsync(c => c.UserId == userId && c.Name == contactName, cancellationToken);
                    
                    if (contact == null)
                    {
                        return new LineReplyStatus
                        {
                            Status = 404,
                            ReplyMessage = new LineReplyMessage
                            {
                                ReplyToken = replyToken,
                                Messages = new List<LineMessage> { new LineTextMessage($"ไม่พบผู้ติดต่อ '{contactName}'") }
                            }
                        };
                    }

                    // Delete contact
                    _context.Contacts.Remove(contact);
                    await _context.SaveChangesAsync(cancellationToken);

                    return new LineReplyStatus
                    {
                        Status = 200,
                        ReplyMessage = new LineReplyMessage
                        {
                            ReplyToken = replyToken,
                            Messages = new List<LineMessage> { new LineTextMessage($"ลบผู้ติดต่อ '{contactName}' เรียบร้อยแล้ว") }
                        }
                    };
                }
                else
                {
                    return new LineReplyStatus
                    {
                        Status = 400,
                        ReplyMessage = new LineReplyMessage
                        {
                            ReplyToken = replyToken,
                            Messages = new List<LineMessage> { new LineTextMessage("กรุณาระบุชื่อผู้ติดต่อที่ต้องการลบ") }
                        }
                    };
                }
            }

            // Handle LIST command
            if (trimmedMessage.Equals("LIST", StringComparison.OrdinalIgnoreCase))
            {
                var contacts = await _context.Contacts
                    .Where(c => c.UserId == userId)
                    .OrderBy(c => c.Name)
                    .ToListAsync(cancellationToken);

                if (contacts.Count == 0)
                {
                    return new LineReplyStatus
                    {
                        Status = 200,
                        ReplyMessage = new LineReplyMessage
                        {
                            ReplyToken = replyToken,
                            Messages = new List<LineMessage> { new LineTextMessage("ไม่มีผู้ติดต่อในระบบ") }
                        }
                    };
                }

                var contactList = string.Join("\n", contacts.Select((c, i) => $"{i + 1}. {c.Name}"));
                return new LineReplyStatus
                    {
                        Status = 200,
                        ReplyMessage = new LineReplyMessage
                        {
                            ReplyToken = replyToken,
                            Messages = new List<LineMessage> { new LineTextMessage($"รายชื่อผู้ติดต่อ:\n{contactList}") }
                        }
                    };
            }

            // Unknown command
            return new LineReplyStatus
            {
                Status = 200,
                ReplyMessage = new LineReplyMessage
                {
                    ReplyToken = replyToken,
                    Messages = new List<LineMessage> 
                    { 
                        new LineTextMessage("คำสั่งไม่ถูกต้อง\nพิมพ์ 'ADD [ชื่อ]' เพื่อเพิ่มผู้ติดต่อ\nพิมพ์ 'DELETE [ชื่อ]' เพื่อลบผู้ติดต่อ\nพิมพ์ 'LIST' เพื่อดูรายชื่อผู้ติดต่อ\nพิมพ์ 'EXIT' เพื่อออกจากโหมดนี้") 
                    }
                }
            };
        }

        private async Task<LineReplyStatus> ProcessGetModeAsync(string message, int chatbotId, string userId, string replyToken, CancellationToken cancellationToken)
        {
            var modeKey = ModeKeyPrefix + userId;
            var trimmedMessage = message?.Trim() ?? "";

            // Handle EXIT command
            if (trimmedMessage.Equals("EXIT", StringComparison.OrdinalIgnoreCase))
            {
                _memoryCache.Remove(modeKey);
                return new LineReplyStatus
                {
                    Status = 200,
                    ReplyMessage = new LineReplyMessage
                    {
                        ReplyToken = replyToken,
                        Messages = new List<LineMessage> { new LineTextMessage("ออกจากโหมดค้นหาไฟล์") }
                    }
                };
            }

            // Handle search query
            if (!string.IsNullOrWhiteSpace(trimmedMessage))
            {
                try
                {
                    // Get chatbot for LLM key
                    var chatbot =
                        await _context.Chatbots.FirstOrDefaultAsync(c => c.Id == chatbotId, cancellationToken);
                    if (chatbot?.LlmKey == null)
                    {
                        return new LineReplyStatus
                        {
                            Status = 500,
                            ReplyMessage = new LineReplyMessage
                            {
                                ReplyToken = replyToken,
                                Messages = new List<LineMessage>
                                {
                                    new LineTextMessage("ระบบยังไม่ได้ตั้งค่า API Key")
                                }
                            }
                        };
                    }

                    // Generate embedding for search query
                    var searchEmbedding =
                        await _openAiService.CallEmbeddingsAsync(trimmedMessage, chatbot.LlmKey, cancellationToken);
                    if (searchEmbedding == null)
                    {
                        return new LineReplyStatus
                        {
                            Status = 500,
                            ReplyMessage = new LineReplyMessage
                            {
                                ReplyToken = replyToken,
                                Messages = new List<LineMessage>
                                {
                                    new LineTextMessage("ไม่สามารถค้นหาได้ กรุณาลองใหม่อีกครั้ง")
                                }
                            }
                        };
                    }

                    // Search for closest files using vector similarity
                    var maxDistance = chatbot.MaximumDistance ?? 2.0;
                    var topK = chatbot.TopKDocument ?? 5;

                    var closestFiles = await _context.TrackFiles
                        .Where(f => f.UserId == userId && f.Embedding != null)
                        .Select(f => new { File = f, Distance = f.Embedding!.L2Distance(searchEmbedding) })
                        .Where(f => f.Distance <= maxDistance)
                        .OrderBy(f => f.Distance)
                        .Take(topK)
                        .ToListAsync(cancellationToken);

                    // Search for files with filename containing the search query
                    var filenameMatches = await _context.TrackFiles
                        .Where(f => f.UserId == userId && f.FileName.Contains(trimmedMessage))
                        .Take(topK)
                        .ToListAsync(cancellationToken);

                    // Combine results: embedding matches first, then filename matches (excluding duplicates)
                    var allMatches = new List<dynamic>();
                    
                    // Add embedding matches
                    foreach (var match in closestFiles)
                    {
                        allMatches.Add(new { File = match.File, Distance = match.Distance, IsFilenameMatch = false });
                    }
                    
                    // Add filename matches that aren't already in the embedding results
                    var embeddingFileIds = closestFiles.Select(f => f.File.Id).ToHashSet();
                    foreach (var file in filenameMatches)
                    {
                        if (!embeddingFileIds.Contains(file.Id))
                        {
                            allMatches.Add(new { File = file, Distance = double.MaxValue, IsFilenameMatch = true });
                        }
                    }

                    if (allMatches.Count == 0)
                    {
                        return new LineReplyStatus
                        {
                            Status = 404,
                            ReplyMessage = new LineReplyMessage
                            {
                                ReplyToken = replyToken,
                                Messages = new List<LineMessage>
                                {
                                    new LineTextMessage($"ไม่พบไฟล์ที่ตรงกับ '{trimmedMessage}'")
                                }
                            }
                        };
                    }

                    // Return the best match
                    var bestMatch = allMatches.First();
                    var fileUrl = $"{_systemService.FullHostName}/information/getfile?id={bestMatch.File.Id}";

                    string responseMessage;
                    if (allMatches.Count == 1)
                    {
                        var matchType = bestMatch.IsFilenameMatch ? "ชื่อไฟล์" : "เนื้อหา";
                        responseMessage = $"พบไฟล์: {bestMatch.File.Description}\nประเภทการค้นหา: {matchType}\nดาวน์โหลด: {fileUrl}";
                    }
                    else
                    {
                        var embeddingCount = allMatches.Count(m => !m.IsFilenameMatch);
                        var filenameCount = allMatches.Count(m => m.IsFilenameMatch);
                        responseMessage =
                            $"พบไฟล์ที่เกี่ยวข้อง {allMatches.Count} ไฟล์\n- ค้นหาด้วยเนื้อหา: {embeddingCount} ไฟล์\n- ค้นหาด้วยชื่อไฟล์: {filenameCount} ไฟล์\nไฟล์ที่ตรงที่สุด: {bestMatch.File.Description}\nดาวน์โหลด: {fileUrl}";
                    }

                    return new LineReplyStatus
                    {
                        Status = 200,
                        ReplyMessage = new LineReplyMessage
                        {
                            ReplyToken = replyToken,
                            Messages = new List<LineMessage> { new LineTextMessage(responseMessage) }
                        }
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to search files for user {UserId} with query {Query}", userId,
                        trimmedMessage);
                    return new LineReplyStatus
                    {
                        Status = 500,
                        ReplyMessage = new LineReplyMessage
                        {
                            ReplyToken = replyToken,
                            Messages = new List<LineMessage>
                            {
                                new LineTextMessage("เกิดข้อผิดพลาดในการค้นหา กรุณาลองใหม่อีกครั้ง")
                            }
                        }
                    };
                }
            }

            // Empty message
            return new LineReplyStatus
            {
                Status = 200,
                ReplyMessage = new LineReplyMessage
                {
                    ReplyToken = replyToken,
                    Messages = new List<LineMessage> { new LineTextMessage("พิมพ์คำค้นหาเพื่อค้นหาไฟล์\nพิมพ์ 'EXIT' เพื่อออกจากโหมดนี้") }
                }
            };
        }

        private async Task<LineReplyStatus> ProcessContactSelectionAsync(string message, string userId, string replyToken, 
            ContactSelectionState contactState, PendingUpload pendingUpload, int chatbotId, CancellationToken cancellationToken)
        {
            var contactSessionKey = ContactSessionKeyPrefix + userId;
            var sessionKey = SessionKeyPrefix + userId;
            
            // Try to find contact by name or number
            Contact? selectedContact = null;
            
            if (int.TryParse(message?.Trim(), out int contactNumber) && 
                contactNumber > 0 && contactNumber <= contactState.Contacts.Count)
            {
                // Select by number
                selectedContact = contactState.Contacts[contactNumber - 1];
            }
            else
            {
                // Find closest matching contact by name
                selectedContact = FindClosestContact(message?.Trim() ?? "", contactState.Contacts);
            }

            if (selectedContact == null)
            {
                return new LineReplyStatus
                {
                    Status = 400,
                    ReplyMessage = new LineReplyMessage
                    {
                        ReplyToken = replyToken,
                        Messages = new List<LineMessage> { new LineTextMessage("ไม่พบผู้ติดต่อที่ระบุ กรุณาลองใหม่อีกครั้ง") }
                    }
                };
            }

            // Associate contact with the file
            contactState.SelectedContactId = selectedContact.Id;
            
            // Continue with file description
            _memoryCache.Remove(contactSessionKey);
            _memoryCache.Set(sessionKey, pendingUpload, TimeSpan.FromMinutes(10));
            
            return new LineReplyStatus
            {
                Status = 200,
                ReplyMessage = new LineReplyMessage
                {
                    ReplyToken = replyToken,
                    Messages = new List<LineMessage> { new LineTextMessage($"เลือกผู้ติดต่อ: {selectedContact.Name}\nกรุณาระบุคำอธิบายไฟล์") }
                }
            };
        }

        private Contact? FindClosestContact(string query, List<Contact> contacts)
        {
            if (string.IsNullOrWhiteSpace(query) || contacts.Count == 0)
                return null;

            // First try exact match
            var exactMatch = contacts.FirstOrDefault(c => 
                c.Name.Equals(query, StringComparison.OrdinalIgnoreCase) ||
                (c.PhoneNumber != null && c.PhoneNumber.Contains(query)));
            
            if (exactMatch != null)
                return exactMatch;

            // Try partial match
            var partialMatch = contacts.FirstOrDefault(c => 
                c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (c.PhoneNumber != null && c.PhoneNumber.Contains(query)));
            
            if (partialMatch != null)
                return partialMatch;

            // Find closest match using Levenshtein distance
            Contact? bestMatch = null;
            int bestDistance = int.MaxValue;

            foreach (var contact in contacts)
            {
                int distance = ComputeLevenshteinDistance(query.ToLower(), contact.Name.ToLower());
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestMatch = contact;
                }
            }

            // Return match if it's reasonably similar (distance <= 3)
            return bestDistance <= 3 ? bestMatch : null;
        }

        private int ComputeLevenshteinDistance(string s1, string s2)
        {
            int[,] d = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                d[i, 0] = i;
            for (int j = 0; j <= s2.Length; j++)
                d[0, j] = j;

            for (int j = 1; j <= s2.Length; j++)
            {
                for (int i = 1; i <= s1.Length; i++)
                {
                    if (s1[i - 1] == s2[j - 1])
                        d[i, j] = d[i - 1, j - 1];
                    else
                        d[i, j] = Math.Min(Math.Min(
                            d[i - 1, j] + 1,    // deletion
                            d[i, j - 1] + 1),   // insertion
                            d[i - 1, j - 1] + 1 // substitution
                        );
                }
            }

            return d[s1.Length, s2.Length];
        }

        public async Task<LineReplyStatus> ProcessLineImageAsync(LineEvent evt, int chatbotId, string messageId,
            string userId, string replyToken, string accessToken, CancellationToken cancellationToken = default)
        {
            // This method should be called when a file is sent from LINE
            // For this example, assume we have a way to get the file bytes from messageId and accessToken
            var (fileContent, contentType) = await TryDownloadLineFileAsync(messageId, accessToken, cancellationToken);
            if (fileContent == null)
            {
                return new LineReplyStatus
                {
                    Status = 400,
                    ReplyMessage = new LineReplyMessage
                    {
                        ReplyToken = replyToken,
                        Messages = new List<LineMessage>
                        {
                            new LineTextMessage("Failed to download file from LINE.")
                        }
                    }
                };
            }

            // Check if user has contacts
            var contacts = await _context.Contacts
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);

            if (contacts.Count > 0)
            {
                // Store file content in cache and ask user to select contact
                var sessionKey = SessionKeyPrefix + userId;
                var contactSessionKey = ContactSessionKeyPrefix + userId;
                
                var pendingUpload = new PendingUpload { FileContent = fileContent, FileExtension = contentType };
                _memoryCache.Set(sessionKey, pendingUpload, TimeSpan.FromMinutes(10));
                
                var contactState = new ContactSelectionState { Contacts = contacts };
                _memoryCache.Set(contactSessionKey, contactState, TimeSpan.FromMinutes(10));
                
                var contactList = string.Join("\n", contacts.Select((c, i) => $"{i + 1}. {c.Name}"));
                return new LineReplyStatus
                {
                    Status = 200,
                    ReplyMessage = new LineReplyMessage
                    {
                        ReplyToken = replyToken,
                        Messages = new List<LineMessage> 
                        { 
                            new LineTextMessage($"กรุณาเลือกผู้ติดต่อ:\n{contactList}\nหรือพิมพ์ชื่อผู้ติดต่อ") 
                        }
                    }
                };
            }
            else
            {
                // No contacts, store file content in cache and wait for user to provide description
                var sessionKey = SessionKeyPrefix + userId;
                _memoryCache.Set(sessionKey, new PendingUpload { FileContent = fileContent, FileExtension = contentType },
                    TimeSpan.FromMinutes(10));
                return new LineReplyStatus
                {
                    Status = 200,
                    ReplyMessage = new LineReplyMessage
                    {
                        ReplyToken = replyToken,
                        Messages = new List<LineMessage> { new LineTextMessage("กรุณาระบุคำอธิบายไฟล์") }
                    }
                };
            }
        }

        // Not used for now
        // If user sends multiple images, ask them to send one by one in Thai
        public Task<LineReplyStatus> ProcessLineImagesAsync(LineEvent mainEvent, int chatbotId,
            List<LineEvent> imageEvents, string userId, string replyToken, string accessToken,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new LineReplyStatus
            {
                Status = 200,
                ReplyMessage = new LineReplyMessage
                {
                    ReplyToken = replyToken,
                    Messages = new List<LineMessage> { new LineTextMessage("กรุณาส่งไฟล์ทีละไฟล์เท่านั้น") }
                }
            });
        }

        public Task<bool> PostProcessLineAsync(string role, string? sourceMessageId, LineSendResponse response,
            bool isForce = false, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        // Updated: Use ReceiptProcessor's GetContentAsync pattern for downloading file from LINE
        private async Task<(byte[]?, string?)> TryDownloadLineFileAsync(string messageId, string accessToken,
            CancellationToken cancellationToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("resilient_nocompress");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                var url = $"https://api-data.line.me/v2/bot/message/{messageId}/content";
                var response = await client.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string errorDetail = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "Failed to get content from LINE API for messageId {MessageId}. Status: {StatusCode}. Body: {Body}",
                        messageId, response.StatusCode, errorDetail);
                    return (null, null);
                }

                byte[] contentBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                string? contentType = response.Content.Headers.ContentType?.MediaType;

                if (string.IsNullOrEmpty(contentType) || contentBytes.Length == 0)
                {
                    _logger.LogWarning("LINE API returned empty content or no content type for messageId: {MessageId}",
                        messageId);
                    return (null, null);
                }

                return (contentBytes, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while downloading file from LINE API");
                return (null, null);
            }
        }

        private static string CalculateFileHash(byte[] fileContent)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(fileContent);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private class PendingUpload
        {
            public byte[]? FileContent { get; set; }
            public string? FileExtension { get; set; }
        }
        
        private class ContactSelectionState
        {
            public List<Contact> Contacts { get; set; } = new List<Contact>();
            public int SelectedContactId { get; set; }
        }
    }
}

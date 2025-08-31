# LineIdentityPreProcessor Implementation Summary

## Overview
I have successfully implemented the `IPreProcessor` interface as requested, specifically creating a `LineIdentityPreProcessor` that:

1. Automatically retrieves user identity information from Google Sheets
2. Appends this information as an OpenAI message next to the system role message
3. Is automatically registered in dependency injection

## Files Created

### 1. Domain Constants
- **File**: `src/Domain/Constants/Systems.cs`
- **Change**: Added `LineIdentityPreProcessor` constant

### 2. Models
- **File**: `src/Infrastructure/Processors/LineIdentityPreProcessor/LineIdentityModels.cs`
- **Purpose**: Defines the `LineUserIdentity` model with properties:
  - LineUserId
  - Initial
  - FirstName
  - LastName
  - Group
  - Faculty
  - Campus

### 3. Google Sheets Helper
- **File**: `src/Infrastructure/Processors/LineIdentityPreProcessor/LineIdentityGoogleSheetHelper.cs`
- **Purpose**: Handles Google Sheets operations:
  - Retrieves user identity by LINE User ID
  - Adds new users when they don't exist
  - Uses the same service account credentials as the existing LLamaPassportProcessor

### 4. Main Processor
- **File**: `src/Infrastructure/Processors/LineIdentityPreProcessor/LineIdentityPreProcessor.cs`
- **Purpose**: Implements `IPreProcessor` interface:
  - Searches for user in Google Sheets by LINE User ID
  - If found, returns formatted identity information as OpenAI message
  - If not found, attempts to get LINE profile and adds new user to sheet
  - Handles errors gracefully and logs appropriately

## How It Works

### Integration with VectorChatService
The `VectorChatService` already has IPreProcessor infrastructure:
- Lines 22, 28, 36-37: Dependency injection setup
- Lines 69-87: Pre-processor execution loop

When a chat completion request is made:
1. System role message is added first
2. All IPreProcessors (including LineIdentityPreProcessor) are executed
3. Each processor can return an OpenAI message that gets appended
4. Normal chat processing continues

### Google Sheets Integration
- **Sheet ID**: `1fo25PBVPeSrvhLpqUL6CNYr4BFHNjCGH0a0CXSBm1hs` (same as LLamaPassportProcessor)
- **Range**: `Sheet1!A:G`
- **Columns**:
  - A: LineUserId
  - B: Initial
  - C: FirstName
  - D: LastName
  - E: Group
  - F: Faculty
  - G: Campus

### Automatic Registration
The processor is automatically registered via the existing DI setup in `DependencyInjection.cs` (lines 142-148) which scans for all IPreProcessor implementations.

## Message Format
When a user identity is found, the processor returns an OpenAI message like:
```
LineUserId = U123456789
Initial = Mr.
FirstName = John
LastName = Doe
Group = Computer Science
Faculty = Engineering
Campus = Main Campus
```

For new users:
```
[New User] LineUserId = U123456789
FirstName = John Doe
(Other fields are empty for new user)
```

## Error Handling
- Graceful failure: If Google Sheets is unavailable, the processor returns null
- Logging: All operations are logged for debugging
- Caching: LINE profile names are cached for 1 hour to reduce API calls

## Benefits
1. **Automatic**: No manual configuration needed
2. **Scalable**: Automatically registers via dependency injection
3. **Robust**: Handles errors without breaking the chat system
4. **Efficient**: Uses caching to minimize external API calls
5. **Consistent**: Follows the same patterns as existing processors

The implementation is now ready and will automatically start working when the application runs.
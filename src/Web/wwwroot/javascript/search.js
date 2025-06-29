// Search and List Page

let chatbotId;
let deleteUrl;
let importMemberUrl;
let removeMemberUrl;
let deleteMessage;
let baseActionUrl;
let toggleUserChatRightUrl;
let currentView = document.getElementById('viewInput');
const searchElement = document.getElementById('Search');

const swalWithBootstrapButtons = Swal.mixin({
    customClass: {
        confirmButton: 'btn bg-gradient-success',
        cancelButton: 'btn bg-gradient-danger'
    },
    showClass: {
        popup: 'animate__animated animate__fadeInDown animate__faster',
        icon: 'animate__animated animate__heartBeat animate__delay-1s'
    },
    hideClass: {
        popup: 'animate__animated animate__fadeOutUp animate__faster',
    },
    buttonsStyling: false
})

function initialize(options) {
    if (options.deleteUrl) {
        deleteUrl = options.deleteUrl;
    }
    if (options.chatbotId) {
        chatbotId = options.chatbotId;
    }
    if (options.deleteMessage) {
        deleteMessage = options.deleteMessage;
    }
    if (options.baseActionUrl) {
        baseActionUrl = options.baseActionUrl;
    }
    if (options.importMemberUrl) {
        importMemberUrl = options.importMemberUrl;
    }
    if (options.removeMemberUrl) {
        removeMemberUrl = options.removeMemberUrl;
    }
    if (options.toggleUserChatRightUrl) {
        toggleUserChatRightUrl = options.toggleUserChatRightUrl;
    }
}

function enterToSearch(event) {
    if (e.code === "Enter") {
        updateSearch();
        e.preventDefault();
        setTimeout(function () {
            document.getElementById('Search').select();
        }, 1000);
    }
}

if (document.getElementsByName('PageSize').length > 0) {
    document.getElementById('pageSelect').value = document.getElementsByName('PageSize')[0].value;
}
document.addEventListener("DOMContentLoaded", function () {
    if (searchElement && !searchElement.hasAttribute('data-keydown-listener')) {
        searchElement.addEventListener("keydown", enterToSearch);
        searchElement.setAttribute('data-keydown-listener', 'true');
    }
});

function hasUnallowedQueryParams(exceptionParams) {
    // Convert all exception parameters to lowercase for case-insensitive comparison
    const lowerCaseExceptions = exceptionParams.map(param => param.toLowerCase());

    // Extract the search query params
    const queryParams = new URLSearchParams(getUrl().substring(1));
console.log(queryParams);
    // Iterate over all query parameters in the URL
    for (const param of queryParams.keys()) {
        // Convert query param key to lowercase for case-insensitive comparison
        const lowerCaseParam = param.toLowerCase();
        
        console.log(lowerCaseParam);
        // Check if the current lowercase parameter is not in the list of lowercase exceptions
        if (!lowerCaseExceptions.includes(lowerCaseParam)) {
            return true; // Found a query param that is not allowed
        }
    }

    // No unallowed query params found
    return false;
}


function swapDayMonthInDates(url) {
    // Split the URL into base and query parts
    const [baseUrl, queryString] = url.split('?');
    if (!queryString) {
        return url; // No query string to process
    }

    // Process each query parameter
    const params = queryString.split('&').map(param => {
        const [key, value] = param.split('=');
        if (key.toLowerCase().includes('date')) {
            // Attempt to parse the date and swap day/month
            const dateParts = decodeURIComponent(value).split('/');
            if (dateParts.length === 3) {
                // Assuming the format is dd/mm/yyyy, swap to mm/dd/yyyy
                let year = parseInt(dateParts[2], 10); // Convert the year part to an integer
                if (year > 2400) {
                    year -= 543;
                } else if (year < 1940) {
                    year += 543;
                }
                let value = encodeURIComponent(`${dateParts[1]}/${dateParts[0]}/${year}`);
                return `${key}=${value}`;
            }
        }
        return param; // Return the original param if not a date or not in expected format
    });

    // Reconstruct the URL with swapped date values
    return `${baseUrl}?${params.join('&')}`;
}

function getUrl() {
    $(".form-select").filter(function () {
        return $(this).data('select2');
    }).select2('destroy');
    let url = baseActionUrl;
    if (document.getElementById('searchForm')) {
        let separator = url.includes('?') ? '&' : '?';
        let inputs = $('#searchForm').serialize(); // Replace '#searchForm' with your form or container ID
        if (inputs) {
            url += separator + inputs;
            separator = '&';
        }
        url += separator + 'NewSearch=True';
        url = url.replace(/\?&/g, '?');
        url = url.replace(/%5B%5D=/g, '=');
        url = url.replace(/=on/g, '=true');
    }
    return swapDayMonthInDates(url);
}

function restoreSearch() {
    $(".form-select").filter(function () {
        return $(this).data('select2');
    }).select2('destroy');

    // Reset all visible input fields except those that are hidden or buttons
    $('#searchForm input:not([type="hidden"]):not([type="button"])').each(function () {
        $(this).val('');
    });

    // Reset all visible select elements
    $('#searchForm select:visible').each(function () {
        $(this).val('').trigger('change');
    });

    $(".form-select").select2({
        theme: 'bootstrap-5',
        language: 'th',
        width: 'resolve'
    });

    updateSearch();
    return false;
}

function updateSearch() {
    refreshPage(getUrl());
    return false;
}

function changePageSize() {
    document.getElementsByName('PageSize')[0].value = document.getElementById('pageSelect').value;
    updateSearch();
}

function navigatePage(page) {
    let url = getUrl();
    url = url.replace('Page=1', 'Page=' + page);
    refreshPage(url);
    return false;
}

function handleCopyOrShare(copyTargetId, copyLinkId) {
    var copyText = document.getElementById(copyTargetId).value;
    var isMobile = /iPhone|iPad|iPod|Android/i.test(navigator.userAgent);

    if (isMobile && navigator.share) {
        // Use Web Share API on mobile devices
        navigator.share({
            title: 'Share Link',
            text: 'Check out this link:',
            url: copyText
        }).then(function() {
            console.log('Successfully shared');
        }).catch(function(err) {
            console.error('Error sharing:', err);
        });
    } else {
        // Use clipboard API on non-mobile devices
        navigator.clipboard.writeText(copyText).then(function() {
            var tooltip = bootstrap.Tooltip.getInstance(document.getElementById(copyLinkId));
            tooltip.setContent({ '.tooltip-inner': 'Copied!' });
            setTimeout(() => {
                tooltip.setContent({ '.tooltip-inner': 'คลิกเพื่อคัดลอก' });
            }, 2000);
        }, function(err) {
            console.error('Async: Could not copy text: ', err);
        });
    }
}

function refreshPage(url) {
    console.log(url);
    let pageSizeInputElement = document.querySelector("input[name='PageSize']");
    let currentPageSize = 20;
    if (pageSizeInputElement) {
        currentPageSize = pageSizeInputElement.value;
        // You can use currentPageSize here as needed
    }
    $.ajax({
        type: "GET",
        url: url,
        success: function (data) {
            let bodyId = "contentBody";
            let searchBarId = "searchHeader";
            let htmlDom = $.parseHTML(data, true);
            let body = $(htmlDom).find('#' + bodyId);
            if (body) {
                $('#' + bodyId).html(body);
            }
            let searchBar = $(htmlDom).find('#' + searchBarId);
            if (searchBar) {
                $('#' + searchBarId).html(searchBar);
            }
            if (document.getElementById('pageSelect')) {
                document.getElementById('pageSelect').value = currentPageSize;
            }
            if (searchElement && !searchElement.hasAttribute('data-keydown-listener')) {
                searchElement.addEventListener("keydown", enterToSearch);
                searchElement.setAttribute('data-keydown-listener', 'true');
            }

            let tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
            if (tooltipTriggerList.length > 0) {
                tooltipTriggerList.map(function (tooltipTriggerEl) {
                    return new bootstrap.Tooltip(tooltipTriggerEl)
                });
            }

            if (document.querySelectorAll('[id^=lineCopyLink]').length > 0) {
                document.querySelectorAll('[id^=lineCopyLink]').forEach(function (element) {
                    element.addEventListener('click', function (e) {
                        e.preventDefault();
                        var itemId = this.getAttribute('data-id');
                        handleCopyOrShare('lineCopyTarget-' + itemId, 'lineCopyLink-' + itemId);
                    });
                });
            }

            setTimeout(() => {
               $(".form-select").select2({
                   theme: 'bootstrap-5',
                   language: 'th',
                   width: 'resolve'
               }, 1000);
           })
        }
    })
    ;
    return false;
}


function downloadJson() {
    window.location = getUrl() + '&exportOptions=json';
}

function switchView() {
    if (currentView.value === 'list' || currentView.value === '') {
        currentView.value = 'grid';
    } else if (currentView.value === 'grid') {
        currentView.value = 'list';
    }
    updateSearch();
}

function confirmDelete(id, description) {
    swalWithBootstrapButtons.fire({
        title: 'ยืนยันการดำเนินการ',
        text: deleteMessage + description,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'ยืนยัน',
        cancelButtonText: 'ยกเลิก',
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                type: 'POST',
                url: deleteUrl,
                data: {
                    Id: id
                },
                success: function (data) {
                    if (document.getElementsByName('Page').length > 0) {
                        let page = document.getElementsByName('Page')[0].innerText;
                        navigatePage(page);
                    } else {
                        navigatePage(1);
                    }
                },
                error: function (error) {
                    // Handle the error
                }
            });
        }
    });
}

let createProvisionUrl;

function initializeCreateProvision(url) {
    createProvisionUrl = url;
}

function createProvision(id, description) {
    swalWithBootstrapButtons.fire({
        title: 'ยืนยันการสร้าง',
        text: "ยืนยันต้องการสร้างบัญชีอนุญาตใช้งาน" + description,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'ยืนยัน',
        cancelButtonText: 'ยกเลิก',
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                type: 'POST',
                url: createProvisionUrl,
                data: {
                    Id: id
                },
                success: function (data) {
                    if (document.getElementsByName('Page').length > 0) {
                        let page = document.getElementsByName('Page')[0].innerText;
                        navigatePage(page);
                    } else {
                        navigatePage(1);
                    }
                },
                error: function (error) {
                    // Handle the error
                }
            });
        }
    });
}

function addMainGroupSearch(keyword) {
    $(".form-select").filter(function () {
        return $(this).data('select2');
    }).select2('destroy');
    document.getElementsByName('MainGroup')[0].value = keyword;
    updateSearch();
}

function addSubGroupSearch(keyword) {
    $(".form-select").filter(function () {
        return $(this).data('select2');
    }).select2('destroy');
    document.getElementsByName('SubGroup')[0].value = keyword;
    updateSearch();
}

function searchByMainGroup() {
    $(".form-select").filter(function () {
        return $(this).data('select2');
    }).select2('destroy');
    document.getElementsByName('SubGroup')[0].value = '';
    updateSearch();
}


// AccessRight.js
let toggleAccessRightUrl;

function initializeToggle(url) {
    toggleAccessRightUrl = url;
}

function toggleAccessRight(id, rightId, rightName) {
    swalWithBootstrapButtons.fire({
        title: 'ยืนยัน',
        text: "ยืนยันการเปลี่ยนสิทธิ์การเข้าถึง" + rightName,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'ยืนยัน',
        cancelButtonText: 'ยกเลิก',
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                type: 'POST',
                url: toggleAccessRightUrl,
                contentType: 'application/json',
                data: JSON.stringify(
                    {
                        UserId: id,
                        RightId: rightId
                    }
                ),
                success: function (data) {
                    updateSearch();
                },
                error: function (error) {
                    // Handle the error
                }
            });
        }
    });
}

// Member to Chatbot
function toggle(source) {
    const checkboxes = document.getElementsByName('Emails[]');
    for (let i = 0; i < checkboxes.length; i++) {
        checkboxes[i].checked = source.checked;
    }
}


function addMember() {
    let elements = document.getElementsByName('Emails[]');;
    let selected = [];
    for (let i = 0; i < elements.length; i++) {
        if (elements[i].checked) {
            selected.push(elements[i].value);
        }
    }
    $.ajax({
        type: "POST",
        url: importMemberUrl,
        contentType: 'application/json',
        data: JSON.stringify({
            "Id": chatbotId,
            "Emails": selected
        }),
        success: function (data) {
            updateSearch();
        }
    });
    return false;   
}

function removeMember() {
    let elements = document.getElementsByName('Emails[]');
    let selected = [];
    for (let i = 0; i < elements.length; i++) {
        if (elements[i].checked) {
            selected.push(elements[i].value);
        }
    }
    $.ajax({
        type: "POST",
        url: removeMemberUrl,
        contentType: 'application/json',
        data: JSON.stringify({
            "Id": chatbotId,
            "Emails": selected
        }),
        success: function (data) {
            updateSearch();
        }
    });
    return false;
}

function toggleUserChatRight(id,email, right) {
    $.ajax({
        type: "POST",
        url: toggleUserChatRightUrl,
        contentType: 'application/json',
        data: JSON.stringify({
            "Id": id,
            "Email": email,
            "Right": right
        }),
        success: function (data) {
            updateSearch();
        }
    });
}

// Code
$(".form-select").select2({
    theme: 'bootstrap-5',
    language: 'th',
    width: 'resolve'
});

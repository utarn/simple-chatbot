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
});

let deleteMessage;
let deleteUrl;
let baseActionUrl;

const searchElement = document.getElementById('Search');

function initialize(options) {
    deleteUrl = options.deleteUrl;
    deleteMessage = options.deleteMessage;
    baseActionUrl = options.baseActionUrl;
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
                    reload();
                },
                error: function (error) {
                    // Handle the error
                }
            });
        }
    });
}

function reload() {
    console.log(baseActionUrl);
    $.ajax({
        type: "GET",
        url: baseActionUrl,
        success: function (data) {
            let bodyId = "contentBody";
            let searchBarId = "searchHeader";
            let htmlDom = $.parseHTML(data, true);
            let body = $(htmlDom).find('#' + bodyId).html();
            let searchBar = $(htmlDom).find('#' + searchBarId).html();
            if (searchBar) {
                $('#' + searchBarId).html(searchBar);
            }

            if (body) {
                $('#' + bodyId).html(body);
            }
            if (document.getElementById('pageSelect')) {
                document.getElementById('pageSelect').value = currentPageSize;
            }

            if (searchElement && !searchElement.hasAttribute('data-keydown-listener')) {
                searchElement.addEventListener("keydown", enterToSearch);
                searchElement.setAttribute('data-keydown-listener', 'true');
            }
            $(".form-select").select2({
                theme: 'bootstrap-5',
                language: 'th',
                width: 'resolve'
            });

        }
    });
}
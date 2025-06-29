// Common part

$(".form-select").select2({
    theme: "bootstrap-5",
    width: "100%",
    allowClear: true,
    placeholder: "เลือก",
    
});


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

const toolbarOptions = [
    ['bold', 'italic', 'underline', 'strike'],        // toggled buttons
    ['blockquote', 'code-block'],

    [{'header': 1}, {'header': 2}],               // custom button values
    [{'list': 'ordered'}, {'list': 'bullet'}],
    [{'script': 'sub'}, {'script': 'super'}],      // superscript/subscript
    [{'indent': '-1'}, {'indent': '+1'}],          // outdent/indent
    [{'direction': 'rtl'}],                         // text direction

    [{'size': ['small', false, 'large', 'huge']}],  // custom dropdown
    [{'header': [1, 2, 3, 4, 5, 6, false]}],

    [{'color': []}, {'background': []}],          // dropdown with defaults from theme
    [{'align': []}],
    ['clean']                                         // remove formatting button
];

var quillText = new Quill('#EditorText', {
    modules: {
        toolbar: toolbarOptions
    },
    theme: 'snow'
});

var quillExplanation = new Quill('#EditorExplanation', {
    modules: {
        toolbar: toolbarOptions
    },
    theme: 'snow'
});

document.getElementById('myForm').addEventListener('submit', function () {
    document.getElementById('Text').value = quillText.root.innerHTML;
    document.getElementById('Explanation').value = quillExplanation.root.innerHTML;
});

window.onload = function () {
    var hiddenTextInput = document.getElementById('Text');
    var hiddenExplanationInput = document.getElementById('Explanation');

    if (hiddenTextInput.value) {
        quillText.root.innerHTML = hiddenTextInput.value;
    }
    if (hiddenExplanationInput.value) {
        quillExplanation.root.innerHTML = hiddenExplanationInput.value;
    }
};

document.getElementById('addMoreFiles').addEventListener('click', function () {
    // Create a new file input element
    var newInput = document.createElement('input');
    newInput.type = 'file';
    newInput.className = 'form-control mt-2'; // Add some margin for spacing
    newInput.accept = "image/jpeg, image/png, image/gif";
    newInput.name = "Files"; // Adjust based on your server-side model

    // Get the form-group of the "Add More Files" button
    var buttonFormGroup = this.parentElement;

    // Insert the new input before the button's form-group
    buttonFormGroup.parentNode.insertBefore(newInput, buttonFormGroup);
});


// Edit part
let deleteMessage;
let deleteUrl;
let baseActionUrl;
const imageListElement = document.getElementById('imageList');

function initialize(options) {
    deleteMessage = options.deleteMessage;
    deleteUrl = options.deleteUrl;
    baseActionUrl = options.baseActionUrl;
}

function confirmDelete(questionId, questionName, fileId) {
    swalWithBootstrapButtons.fire({
        title: 'ยืนยันการดำเนินการ',
        text: deleteMessage + questionName,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'ยืนยัน',
        cancelButtonText: 'ยกเลิก',
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                type: 'POST',
                url: deleteUrl,
                contentType: 'application/json',
                data: JSON.stringify({
                    FileId: fileId,
                    ReferenceId: questionId
                }),
                success:  function (data) {
                    let htmlDom = $.parseHTML(data, true);
                    let imageList = $(htmlDom).find('#imageList');
                    $('#imageList').html(imageList);
                },
                error: function (error) {
                    // Handle the error
                }
            });
        }
    });
}
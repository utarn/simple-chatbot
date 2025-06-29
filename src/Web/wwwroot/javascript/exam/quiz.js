let state = {
    takeExamId: null,
    remainingTime: null,
    currentOrder: null,
    totalQuestion: null,
    unansweredQuestion: null,
    questionData: null,
    questionStatus: null,
    examMode: null,
    showAnswer: false,
    selectedChoiceId: [],
    selectedConfidenceLevel: null,
    intervalTime: null,
    reconnectAttempts: 0,
    maxReconnectAttempts: 5
};

const elements = {
    breakExam: document.getElementById('breakExam'),
    confidenceLow: document.getElementById('confidenceLow'),
    confidenceMedium: document.getElementById('confidenceMedium'),
    confidenceHigh: document.getElementById('confidenceHigh'),
    prevBtn: document.getElementById('prevBtn'),
    nextBtn: document.getElementById('nextBtn'),
    selectedConfidenceLabel: document.getElementById('selectedConfidenceLabel'),
    questionStatusModal: document.getElementById('questionStatusModal'),
    countdownTimer: document.getElementById('countdownTimer')
};

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

elements.prevBtn.addEventListener('click', previousQuestion);
elements.nextBtn.addEventListener('click', moveNextStep);
elements.confidenceLow.addEventListener('click', () => postAnswerData(-1));
elements.confidenceMedium.addEventListener('click', () => postAnswerData(0));
elements.confidenceHigh.addEventListener('click', () => postAnswerData(1));
elements.breakExam.addEventListener('click', breakExam);
elements.questionStatusModal.addEventListener('show.bs.modal', loadQuestionStatus);
$(document).on('click', '.choice', updateChoice);

const connection = new signalR.HubConnectionBuilder()
    .withUrl('/status')
    .withHubProtocol(new signalR.protocols.msgpack.MessagePackHubProtocol())
    .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: retryContext => {
            state.reconnectAttempts++;
            if (state.reconnectAttempts <= state.maxReconnectAttempts) {
                if (retryContext.elapsedMilliseconds < 60000) {
                    return Math.random() * 3000;
                } else {
                    return 1000;
                }
            } else {
                // Exceeded max reconnect attempts, show the error message
                setTimeout(async () => {
                    await swalWithBootstrapButtons.fire({
                        title: 'โหลดหน้านี้ใหม่',
                        text: 'ไม่สามารถเชื่อมต่อกับเซิร์ฟเวอร์ได้ กรุณาลองอีกครั้ง',
                        icon: 'error',
                        showCancelButton: false,
                        confirmButtonText: 'รับทราบ'
                    });
                    window.location.reload();
                    return null;
                }, 0);
                return null;
            }
        }
    })
    .build();

async function previousQuestion() {
    if (state.currentOrder > 1) {
        state.currentOrder--;
        resetQuestionState();
        await getQuestion();
    }
    updateProgressBar();
    updateNavigationButton();
}

async function nextQuestion() {
    if (state.currentOrder < state.totalQuestion) {
        state.currentOrder++;
        resetQuestionState();
        await getQuestion();
    }
    updateProgressBar();
    updateNavigationButton();
}

function resetQuestionState() {
    state.showAnswer = false;
    state.selectedChoiceId = [];
    state.selectedConfidenceLevel = null;
    elements.selectedConfidenceLabel.innerText = '';
}

function updateNavigationButton() {
    elements.prevBtn.disabled = state.currentOrder === 1;
    elements.nextBtn.disabled = state.currentOrder === state.totalQuestion;
    elements.breakExam.innerText = state.currentOrder !== state.totalQuestion ? "หยุดพักการสอบ" : "ส่งคำตอบ";
    if (state.showAnswer) {
        elements.confidenceLow.disabled = true;
        elements.confidenceMedium.disabled = true;
        elements.confidenceHigh.disabled = true;
    }
}

async function moveNextStep() {
    disableConfidenceButtons();
    if (state.examMode === 0 && state.selectedChoiceId.length > 0 && state.showAnswer === false) {
        state.showAnswer = true;
        await getQuestion();
    } else if (state.examMode === 0) {
        state.showAnswer = false;
        if (state.currentOrder < state.totalQuestion) {
            await nextQuestion();
        }
    } else if (state.examMode === 1) {
        state.showAnswer = false;
        if (state.currentOrder < state.totalQuestion) {
            await nextQuestion();
        } else {
            let data = await fetchQuestionStatus();
            const firstUnanswered = data.find(element => element.isAnswered === false);
            if (firstUnanswered) {
                state.currentOrder = firstUnanswered.order;
                await getQuestion();
            }
        }
    }
}

async function postAnswerData(confidenceLevel) {
    if (state.selectedChoiceId.length > 0 && confidenceLevel !== null) {
        let postData = {
            takeExamId: state.takeExamId,
            order: state.currentOrder,
            answer: state.selectedChoiceId,
            confidenceLevel: confidenceLevel
        };
        let encoded = MessagePack.encode(postData)
        let data = await connection.invoke("UpdateAnswer", encoded);
        state.unansweredQuestion = parseInt(data, 10);
        if (state.unansweredQuestion === 0) {
            const answerResult = await swalWithBootstrapButtons.fire({
                title: 'คุณทำครบทุกข้อแล้ว',
                text: ' คุณต้องการส่งคำตอบทั้งหมดหรือไม่',
                icon: 'info',
                showCancelButton: true,
                confirmButtonText: 'ยืนยัน',
                cancelButtonText: 'ยกเลิก',
            });
            if (answerResult.isConfirmed) {
                clearInterval(state.intervalTime);
                window.location.href = '/Exam/Finish?TakeExamId=' + state.takeExamId;
            } else {
                await getQuestion();
            }
        } else {
            await moveNextStep();
        }
    } else {
        console.log("Selected choice or confidence level is not set.");
    }
}

async function breakExam() {
    const message = state.currentOrder === state.totalQuestion
        ? (state.unansweredQuestion !== 0
            ? 'คุณยังไม่ได้ส่งคำตอบจำนวน ' + state.unansweredQuestion + 'ข้อ'
            : 'คุณต้องการส่งคำตอบทั้งหมดหรือไม่')
        : 'กำลังสอบอยู่';

    let result = await swalWithBootstrapButtons.fire({
        title: message,
        text: ' คุณต้องการหยุดพักการสอบหรือไม่',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'ยืนยัน',
        cancelButtonText: 'ยกเลิก',
    });
    if (result.isConfirmed) {
        clearInterval(state.intervalTime);
        let updateResult = await updateTime('true')
        if (updateResult === false && typeof updateResult === 'boolean') {
            window.location.href = '/Exam/Index'; // Redirect if result is false
        }
    }
}

function initialize(option) {
    Object.assign(state, option);
    initializeCountdown(state.remainingTime);
    updateNavigationButton();
}

function initializeCountdown(seconds) {
    let targetTime = new Date().getTime() + seconds * 1000;

    state.intervalTime = setInterval(function () {
        let currentTime = new Date().getTime();
        let distance = targetTime - currentTime;
        let hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
        let minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
        let seconds = Math.floor((distance % (1000 * 60)) / 1000);

        elements.countdownTimer.innerHTML = padZero(hours) + ":" + padZero(minutes) + ":" + padZero(seconds);

        if (distance < 0) {
            clearInterval(state.intervalTime);
            elements.countdownTimer.innerHTML = "หมดเวลา";
        }
    }, 1000);
}

function updateChoice() {
    let isMultipleAnswer = state.questionData?.masterQuestion?.isMultipleAnswer ?? false;
    if (state.examMode === 0 && state.showAnswer === true) {
        return;
    }
    if (isMultipleAnswer) {
        this.classList.toggle('bg-info');
        const choiceId = parseInt($(this).data('choice-id'));
        if (this.classList.contains('bg-info')) {
            if (!state.selectedChoiceId.includes(choiceId)) {
                state.selectedChoiceId.push(choiceId);
            }
        } else {
            state.selectedChoiceId = state.selectedChoiceId.filter(id => id !== choiceId);
        }
    } else {
        $('.choice').removeClass('bg-info');
        $(this).addClass('bg-info');
        const choiceId = parseInt($(this).data('choice-id'));
        state.selectedChoiceId = [];
        if (!state.selectedChoiceId.includes(choiceId)) {
            state.selectedChoiceId.push(choiceId);
        }
    }
    if (state.selectedChoiceId.length > 0) {
        enableConfidenceButtons();
    }
}
function enableConfidenceButtons() {
    document.querySelectorAll('.confidence-btn')
        .forEach(button => button.disabled = false);
}

function disableConfidenceButtons() {
    document.querySelectorAll('.confidence-btn')
        .forEach(button => button.disabled = true);
}

function updateQuestionnaire() {
    // Update question text
    $('#questionText').html(state.questionData.masterQuestion.text);

    // Update question images
    var imagesContainer = document.getElementById('questionImages'); // Get the container for images
    imagesContainer.innerHTML = ''; // Clear existing content

    state.questionData.masterQuestion.base64Images.forEach(function (image) {
        // Create a div for each image
        var imageDiv = document.createElement('div');

        // Create an img element
        var img = document.createElement('img');
        img.src = 'data:image/' + image.fileType + ';base64,' + image.base64;
        img.classList.add('img');
        img.classList.add('img-fluid');
        img.classList.add('ps-3');
        img.classList.add('pe-3');

        // Append the img to the div
        imageDiv.appendChild(img);

        // Append the div to the container
        imagesContainer.appendChild(imageDiv);
    });

    if (state.examMode === 0 && state.questionData.selectedAnswer && state.questionData.selectedAnswer.length > 0 && state.questionData.confidentLevel !== null) {
        state.showAnswer = true;
    }
    if (state.questionData.selectedAnswer && state.questionData.selectedAnswer.length > 0) {
        enableConfidenceButtons();
    }

    if (state.showAnswer && state.questionData.masterQuestion.explanation !== null && state.questionData.masterQuestion.explanation !== undefined) {
        $('#questionExplanation').show(); // Show the explanation section
        $('#questionExplanation .card-body').html('<span class="font-weight-bold">คำอธิบาย:</span><br/>' + state.questionData.masterQuestion.explanation); // Update the explanation content
    } else {
        $('#questionExplanation').hide(); // Hide the explanation section
    }
    // Clear existing choices
    $('#choiceList').empty();

    // Dynamically create and append choices
    state.questionData.masterQuestion.answers.forEach(function (answer, index) {
        var choiceNum = index + 1; // Start numbering choices from 1
        var choiceId = 'choice' + choiceNum;
        var explanationId = 'explanation' + choiceNum; // Unique ID for accordion collapse control
        var isSelected = state.questionData.selectedAnswer.includes(answer.id); // Check if the current answer ID is in the selectedAnswer array
        var choiceHtml = '<div id="' + choiceId + '" class="card card-body choice p-1 ms-2 me-2 ms-md-3 me-md-3 rounded-rectangle choice' + (isSelected ? ' bg-info' : '') + '" data-choice-id="' + answer.id + '">' +
            '<div class="text-center">' + answer.text + '</div>';

        // Append choice images if any
        answer.base64Images.forEach(function (image) {
            choiceHtml += '<div class="image-container">';
            choiceHtml += '<img src="data:image/' + image.fileType + ';base64,' + image.base64 + '" class="img img-fluid ps-3 pe-3">';
            choiceHtml += '</div>'; // End of image container div
        });

        if (state.showAnswer && answer.explanation !== null && answer.explanation !== undefined) {
            var explanationClass = answer.score > 0 ? 'bg-success-subtle' : 'bg-danger-subtle';
            choiceHtml += '<div class="accordion-item p-1">' +
                '<h2 class="accordion-header" id="heading' + choiceNum + '">' +
                '<button class="accordion-button p-2 mb-0 bg-white btn bg-gradient-secondary collapsed" style="width:90px" type="button" data-bs-toggle="collapse" data-bs-target="#' + explanationId + '" aria-expanded="false" aria-controls="' + explanationId + '">' +
                'คำอธิบาย' +
                '</button>' +
                '</h2>' +
                '<div id="' + explanationId + '" class="accordion-collapse collapse" aria-labelledby="heading' + choiceNum + '">' +
                '<div class="accordion-body text-dark ' + explanationClass + '">' +
                answer.explanation +
                '</div>' +
                '</div>' +
                '</div>';
        }

        choiceHtml += '</div>'; // Close the choice container div

        // Append the constructed choice HTML to the choice list
        $('#choiceList').append(choiceHtml);
    });

    // Update the global variable to reflect the selected answers as an array
    state.selectedChoiceId = state.questionData.selectedAnswer || [];

    // Handle the confidence level
    if (state.questionData.confidentLevel || state.questionData.confidentLevel === 0) {
        // Remove any existing highlights from confidence buttons
        $('.confidence-btn').removeClass('bg-info');
        // Define a mapping between confidentLevel values and button IDs
        let confidenceMap = {
            '-1': 'confidenceLow',
            '0': 'confidenceMedium',
            '1': 'confidenceHigh'
        };
        // Optional: Display the label of the selected confidence level
        // For example, if you want to display the label in a div with id="selectedConfidenceLabel"
        // console.log("questionData confidentLevel: ", questionData.confidentLevel);
        elements.selectedConfidenceLabel.innerHTML = ': ' + document.getElementById(confidenceMap[state.questionData.confidentLevel.toString()]).textContent;
    } else {
        elements.selectedConfidenceLabel.innerText = '';
    }
}

async function getQuestion() {
    $('#loadingModal').modal('show');
    while (connection.state !== signalR.HubConnectionState.Connected) {
        await new Promise(resolve => setTimeout(resolve, 1000));
    }

    // Use SignalR's 'invoke' method to call the 'GetQuestionInExam' method on the hub
    let jsonData = {
        takeExamId: state.takeExamId,
        order: state.currentOrder
    }
    let encoded = MessagePack.encode(jsonData)
    try {
        state.questionData = await connection.invoke("GetQuestionInExam", encoded);
        updateQuestionnaire();
        updateProgressBar();
        if (state.examMode === 0
            && state.questionData.selectedAnswer.length > 0
            && state.questionData.confidentLevel !== null) {
            state.showAnswer = true;
        }
        updateNavigationButton();
        $('#loadingModal').modal('hide');
    } catch (err) {
        $('#loadingModal').modal('hide');
        console.error("Error fetching data: ", err);
    }
}

function updateProgressBar() {
    var percentage = (state.currentOrder / state.totalQuestion) * 100;
    var progressBar = document.getElementById('progressBar');
    progressBar.style.width = percentage + '%';
    progressBar.setAttribute('aria-valuenow', percentage);
    progressBar.innerHTML = '<h4>' + state.currentOrder + '/' + state.totalQuestion + '</h4>';
}

async function start() {
    try {
        await connection.start();
        console.log("SignalR Connected.");
        state.reconnectAttempts = 0;
        state.intervalTime = setInterval(async () => {
            try {
                const result = await updateTime('false');
                if (result === false) {
                    clearInterval(state.intervalTime);
                    window.location.href = '/Exam/Finish?TakeExamId=' + state.takeExamId;
                }
            } catch (error) {
                console.error('Error updating time:', error);
                clearInterval(state.intervalTime);
            }
        }, 2000);
    } catch (err) {
        console.log(err);
        setTimeout(start, 5000);
    }
}

connection.onclose(async () => {
    await start();
});

function padZero(number) {
    return number < 10 ? '0' + number : number;
}


async function updateTime(status) {
    while (connection.state !== signalR.HubConnectionState.Connected) {
        console.log('waiting for connection');
        await new Promise(resolve => setTimeout(resolve, 1000));
    }
    try {
        return await connection.invoke("UpdateTime", state.takeExamId, status);
    } catch (err) {
        console.error(err.toString());
    }
}


// Function to load and display question status
async function fetchQuestionStatus() {
    try {
        // Assuming 'connection' is your initialized and started SignalR connection
        return await connection.invoke("GetQuestionStatus", state.takeExamId); // This is the data returned from the SignalR server method
    } catch (error) {
        console.error('Error fetching question status:', error);
    }
}

async function loadQuestionStatus() {
    try {
        while (connection.state !== signalR.HubConnectionState.Connected) {
            await new Promise(resolve => setTimeout(resolve, 1000));
        }
        const data = await connection.invoke("GetQuestionStatus", state.takeExamId);
        let contentHtml = '<div class="row justify-content-start mb-2">'; 
        for (let i = 0; i < data.length; i++) {
            let badgeClass = 'bg-secondary';
            if (data[i].isAnswered) {
                badgeClass = 'bg-info';
            }
            if (!data[i].isAnswered) {
                badgeClass = 'bg-danger';
            }
            if (data[i].order === state.currentOrder && data[i].isAnswered) {
                badgeClass = 'bg-info badge-custom-info-border';
            }
            if (data[i].order === state.currentOrder && !data[i].isAnswered) {
                badgeClass = 'bg-danger badge-custom--danger-border';
            }
            contentHtml += '<div class="col-auto"><span class="badge ' + badgeClass + ' cursor-pointer" onclick="gotoQuestion(' + data[i].order + ');">' + data[i].order + '</span></div>';
        }
        contentHtml += '</div>'; 

        contentHtml += '<div class="text-center mt-3">' +
            '<button type="button" class="btn btn-secondary" data-bs-dismiss="modal">ปิด</button>' +
            '</div>';

        $('#questionStatusBody').html(contentHtml);
    } catch (error) {
        console.error('Error fetching question status:', error);
    }
}

async function gotoQuestion(order) {
    $('#questionStatusModal').modal('hide');
    if (state.currentOrder !== order) {
        state.currentOrder = order;
        state.showAnswer = false;
        state.selectedChoiceId = [];
        state.selectedConfidenceLevel = null;
        elements.selectedConfidenceLabel.innerText = '';
        await getQuestion();
        updateNavigationButton();
    }
}
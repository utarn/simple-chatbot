let myChart = null;
function getLegendFontSize() {
    let screenWidth = window.innerWidth;

    // Bootstrap breakpoints
    if (screenWidth < 576) { // Extra small devices
        return 10; // Smallest font size for extra small devices
    } else if (screenWidth >= 576 && screenWidth < 768) { // Small devices
        return 12; // Slightly larger font size for small devices
    } else if (screenWidth >= 768 && screenWidth < 992) { // Medium devices
        return 14; // Medium font size for medium devices
    } else if (screenWidth >= 992 && screenWidth < 1200) { // Large devices
        return 16; // Larger font size for large devices
    } else if (screenWidth >= 1200 && screenWidth < 1400) { // Extra large devices
        return 18; // Even larger font size for extra large devices
    } else { // Extra extra large devices
        return 22; // Largest font size for extra extra large devices
    }
}

function initializeChart(chartData) {
    let options = {
        plugins: {
            legend: {
                labels: {
                    // This more specific font property overrides the global property
                    font: {
                        size: getLegendFontSize()
                    }
                }
            }
        },
        scales: {
            y: {
                beginAtZero: true,
                position: 'left',
                ticks: {
                    // Convert the tick value to a string with a percentage symbol
                    callback: function (value, index, values) {
                        return value.toLocaleString() + '%'; // Convert the number to a string and append '%'
                    }
                },
                // Set the maximum value of the y-axis to 100%
                suggestedMax: 100
            },
            y1: {
                beginAtZero: true, // Not necessary to start at zero for this data range
                position: 'right',
                ticks: {
                    // Format ticks to show from -1 to 1
                    callback: function (value, index, values) {
                        return value; // No special formatting needed
                    }
                },
                min: -1, // Set minimum value of y1 axis to -1
                max: 1,  // Set maximum value of y1 axis to 1
                grid: {
                    drawOnChartArea: true, // Prevent grid lines from overlapping with the left y-axis
                }
            }
        }
    };
    let ctx = document.getElementById('examStatsChart').getContext('2d');
    if (myChart) {
        myChart.options = options;
        myChart.update();
    } else {
        myChart = new Chart(ctx, {
            type: 'line',
            options: options,
            data: {
                labels: chartData.labels, // Use the property from the chartData object
                datasets: [{
                    label: 'สัดส่วนข้อถุก',
                    data: chartData.percentageCorrectData, // Use the property from the chartData object
                    borderColor: 'rgb(75, 192, 192)',
                    yAxisID: 'y',
                }, {
                    label: 'สัตส่วนข้อผิด',
                    data: chartData.percentageWrongData, // Use the property from the chartData object
                    borderColor: 'rgb(255, 99, 132)',
                    yAxisID: 'y',
                }, {
                    label: 'ความมั่นใจเฉลี่ย',
                    data: chartData.averageConfidenceLevelData, // Use the property from the chartData object
                    borderColor: 'rgb(153, 102, 255)',
                    yAxisID: 'y1',
                    borderDash: [5, 5],
                }]
            }
        });
    }
}
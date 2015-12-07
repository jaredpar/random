google.load('visualization', '1.0', { 'packages': ['corechart'] });
google.setOnLoadCallback(function () {
    $(document).ready(drawChart);
});

function drawChart() {
    var elem = $('#issuechart');
    var data = [['User', 'Issue Count']];

    var dataPairs = elem.attr('data-values').split(';');
    dataPairs.forEach(function (str, _, _) {
        var item = str.split('-');
        data.push([item[0], parseInt(item[1])]);
    });

    var options = {
        title: "User breakdown"
    };

    var chart = new google.visualization.PieChart(elem.get(0));
    var dataTable = google.visualization.arrayToDataTable(data);
    chart.draw(dataTable, options);
}

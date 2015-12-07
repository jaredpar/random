$(document).ready(function () {
    google.load("visualization", "1", { packages: ["corechart"] });
    google.setOnLoadCallback(drawChart);

    function drawChart() {
        var elem = $('#issuechart');
        var data = [['User', 'Issue Count']];

        /*
        var dataPairs = elem.attr('data-values').split(';');
        dataPairs.foreach(function (str, _, _) {
            var item = str.split('-');
            data.push([item[0], parseInt(item[1])]);
        });
        */

        var options = {
            title: "User breakdown"
        };

        var chart = new google.visualization.PieChart(elem.get(0));
        chart.draw(data, options);
    }
});

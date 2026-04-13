window.activityChart = {
    _chart: null,

    initialize: function (canvasId, chartData) {
        var ctx = document.getElementById(canvasId);
        if (!ctx) return;

        if (this._chart) {
            this._chart.destroy();
            this._chart = null;
        }

        this._chart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: chartData.labels,
                datasets: chartData.datasets
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: {
                    mode: 'index',
                    intersect: false
                },
                plugins: {
                    legend: {
                        position: 'top',
                        labels: {
                            usePointStyle: true,
                            padding: 15
                        }
                    },
                    tooltip: {
                        mode: 'index',
                        intersect: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            precision: 0
                        }
                    }
                }
            }
        });
    },

    update: function (chartData) {
        if (!this._chart) return;

        this._chart.data.labels = chartData.labels;
        this._chart.data.datasets = chartData.datasets;
        this._chart.update('none');
    },

    dispose: function () {
        if (this._chart) {
            this._chart.destroy();
            this._chart = null;
        }
    }
};

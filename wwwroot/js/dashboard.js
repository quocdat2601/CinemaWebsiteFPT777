// Dashboard JavaScript functionality
// Reusable Chart.js initialization function
function createResponsiveChart(canvas, config) {
    if (!canvas) return null;
    // Always destroy previous chart if present
    if (canvas.chartInstance) canvas.chartInstance.destroy();
    config.options = config.options || {};
    config.options.responsive = true;
    config.options.maintainAspectRatio = false;
    const chart = new Chart(canvas, config);
    canvas.chartInstance = chart;
    return chart;
}

// Load dashboard data from hidden element
function getDashboardData() {
    const el = document.getElementById('dashboard-data');
    if (!el) return null;
    try {
        return JSON.parse(el.textContent);
    } catch (e) {
        console.error('Failed to parse dashboard data:', e);
        return null;
    }
}

// Tab switching logic
function showTab(tab) {
    const movieTab = document.getElementById('tab-movie');
    const foodTab = document.getElementById('tab-food');
    const movieDash = document.getElementById('movie-dashboard');
    const foodDash = document.getElementById('food-dashboard');
    const downloadBtn = document.getElementById('download-charts-btn');

    if (tab === 'movie') {
        movieTab.classList.add('active');
        foodTab.classList.remove('active');
        movieDash.style.display = '';
        foodDash.style.display = 'none';
        downloadBtn.onclick = downloadMovieCharts;
        const data = getDashboardData();
        if (data) window.initMovieCharts(data);
    } else {
        foodTab.classList.add('active');
        movieTab.classList.remove('active');
        foodDash.style.display = '';
        movieDash.style.display = 'none';
        downloadBtn.onclick = downloadFoodCharts;
        const data = getDashboardData();
        if (data) {
            window.foodAnalyticsData = data.FoodAnalytics;
            window.initFoodCharts();
            setTimeout(() => {
                renderFoodItemsByCategoryBar();
                renderTopBottomStackedBar();
            }, 0);
        }
    }
}

// Chart.js initialization for movie stats
window.initMovieCharts = function(data) {
    const revenueLabels = data.RevenueTrendDates || [];
    const revenueData = data.RevenueTrendValues || [];
    const bookingLabels = data.BookingTrendDates || [];
    const bookingData = data.BookingTrendValues || [];
    const voucherData = data.VoucherTrendValues || [];
    const topMoviesData = data.TopMovies || [];
    const topMembersData = data.TopMembers || [];

    // Combined chart for movie stats - Three Bucket Pattern
    const comboCanvas = document.getElementById('movieComboChart');
    if (comboCanvas && revenueLabels.length && bookingLabels.length) {
        window.movieComboChartObj = createResponsiveChart(comboCanvas, {
            type: 'bar',
            data: {
                labels: revenueLabels.map(d => {
                    // Format date as MM-dd
                    const date = new Date(d);
                    return (date.getMonth()+1).toString().padStart(2,'0') + '-' + date.getDate().toString().padStart(2,'0');
                }),
                datasets: [
                    {
                        type: 'bar',
                        label: 'Gross Seat Revenue',
                        data: revenueData,
                        backgroundColor: '#FFD700',
                        yAxisID: 'y',
                        order: 4
                    },
                    {
                        type: 'bar',
                        label: 'Vouchers Issued',
                        data: voucherData,
                        backgroundColor: '#dc3545',
                        yAxisID: 'y',
                        order: 3
                    },
                    {
                        type: 'bar',
                        label: 'Net Seat Revenue',
                        data: revenueData.map((rev, i) => rev - (voucherData[i] || 0)),
                        backgroundColor: '#28a745',
                        yAxisID: 'y',
                        order: 2
                    },
                    {
                        type: 'line',
                        label: 'Bookings',
                        data: bookingData,
                        borderColor: '#007bff',
                        backgroundColor: 'transparent',
                        fill: false,
                        yAxisID: 'y1',
                        tension: 0.3,
                        pointRadius: 4,
                        pointBackgroundColor: '#007bff',
                        order: 1
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: {
                    mode: 'index',
                    intersect: false,
                },
                scales: {
                    x: {
                        display: true,
                        title: {
                            display: true,
                            text: 'Date'
                        }
                    },
                    y: {
                        type: 'linear',
                        display: true,
                        position: 'left',
                        title: {
                            display: true,
                            text: 'Revenue (VND)'
                        },
                        ticks: {
                            callback: function(value) {
                                return value.toLocaleString('vi-VN') + ' đ';
                            }
                        }
                    },
                    y1: {
                        type: 'linear',
                        display: true,
                        position: 'right',
                        title: {
                            display: true,
                            text: 'Bookings'
                        },
                        grid: {
                            drawOnChartArea: false,
                        },
                        ticks: {
                            callback: function(value) {
                                return Number.isInteger(value) ? value : '';
                            },
                            stepSize: 1
                        }
                    }
                },
                plugins: {
                    title: {
                        display: true,
                        text: 'Revenue, Vouchers & Bookings (Last 7 days)'
                    },
                    legend: {
                        display: true,
                        position: 'top'
                    }
                }
            }
        });
    }

    // Top movies chart
    const topMoviesCanvas = document.getElementById('topMoviesChart');
    if (topMoviesCanvas && topMoviesData.length) {
        createResponsiveChart(topMoviesCanvas, {
            type: 'doughnut',
            data: {
                labels: topMoviesData.map(m => m.Item1),
                datasets: [{
                    data: topMoviesData.map(m => m.Item2),
                    backgroundColor: [
                        '#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF'
                    ],
                    borderWidth: 2,
                    borderColor: '#fff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: {
                        display: true,
                        text: 'Top 5 Movies by Tickets Sold'
                    },
                    legend: {
                        position: 'bottom'
                    }
                }
            }
        });
    }

    // Top members chart
    const topMembersCanvas = document.getElementById('topMembersChart');
    if (topMembersCanvas && topMembersData.length) {
        createResponsiveChart(topMembersCanvas, {
            type: 'doughnut',
            data: {
                labels: topMembersData.map(m => m.Item1),
                datasets: [{
                    data: topMembersData.map(m => m.Item2),
                    backgroundColor: [
                        '#FF9F40', '#4BC0C0', '#36A2EB', '#FF6384', '#C9CBCF'
                    ],
                    borderWidth: 2,
                    borderColor: '#fff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: {
                        display: true,
                        text: 'Top 5 Members by Bookings'
                    },
                    legend: {
                        position: 'bottom'
                    }
                }
            }
        });
    }
};

// Food charts initialization
window.initFoodCharts = function() {
    const data = window.foodAnalyticsData;
    if (!data) return;

    const revenueByDayLabels = data.RevenueByDayDates?.map(d => {
        const date = new Date(d);
        return (date.getMonth()+1).toString().padStart(2,'0') + '-' + date.getDate().toString().padStart(2,'0');
    }) || [];
    const revenueByDay = data.RevenueByDayValues || [];
    const voucherTrendValues = data.VoucherTrendValues || [];
    const netRevenueByDay = data.NetRevenueByDayValues || [];
    const ordersByDay = data.OrdersByDayValues || [];

    // Food combo chart
    const foodComboCanvas = document.getElementById('foodComboChart');
    if (foodComboCanvas && revenueByDayLabels.length) {
        window.foodComboChartObj = createResponsiveChart(foodComboCanvas, {
            type: 'bar',
            data: {
                labels: revenueByDayLabels,
                datasets: [
                    {
                        type: 'bar',
                        label: 'Gross Revenue',
                        data: revenueByDay,
                        backgroundColor: '#FFD700',
                        yAxisID: 'y',
                        order: 4
                    },
                    {
                        type: 'bar',
                        label: 'Vouchers Issued',
                        data: voucherTrendValues,
                        backgroundColor: '#dc3545',
                        yAxisID: 'y',
                        order: 3
                    },
                    {
                        type: 'bar',
                        label: 'Net Revenue',
                        data: netRevenueByDay,
                        backgroundColor: '#28a745',
                        yAxisID: 'y',
                        order: 2
                    },
                    {
                        type: 'line',
                        label: 'Orders',
                        data: ordersByDay,
                        borderColor: '#007bff',
                        backgroundColor: 'transparent',
                        fill: false,
                        yAxisID: 'y1',
                        tension: 0.3,
                        pointRadius: 4,
                        pointBackgroundColor: '#007bff',
                        order: 1
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: {
                    mode: 'index',
                    intersect: false,
                },
                scales: {
                    x: {
                        display: true,
                        title: {
                            display: true,
                            text: 'Date'
                        }
                    },
                    y: {
                        type: 'linear',
                        display: true,
                        position: 'left',
                        title: {
                            display: true,
                            text: 'Revenue (VND)'
                        },
                        ticks: {
                            callback: function(value) {
                                return value.toLocaleString('vi-VN') + ' đ';
                            }
                        }
                    },
                    y1: {
                        type: 'linear',
                        display: true,
                        position: 'right',
                        title: {
                            display: true,
                            text: 'Orders'
                        },
                        grid: {
                            drawOnChartArea: false,
                        },
                        ticks: {
                            callback: function(value) {
                                return Number.isInteger(value) ? value : '';
                            },
                            stepSize: 1
                        }
                    }
                },
                plugins: {
                    title: {
                        display: true,
                        text: 'Revenue, Vouchers & Orders (Last 7 days)'
                    },
                    legend: {
                        display: true,
                        position: 'top'
                    }
                }
            }
        });
    }

    // Food category pie chart
    const foodCategoryCanvas = document.getElementById('foodCategoryPie');
    if (foodCategoryCanvas && data.SalesByCategory) {
        const categoryData = data.SalesByCategory;
        createResponsiveChart(foodCategoryCanvas, {
            type: 'doughnut',
            data: {
                labels: categoryData.map(c => c.Item1),
                datasets: [{
                    data: categoryData.map(c => c.Item2),
                    backgroundColor: [
                        '#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF',
                        '#FF9F40', '#C9CBCF', '#FF6384', '#36A2EB', '#FFCE56'
                    ],
                    borderWidth: 2,
                    borderColor: '#fff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: {
                        display: true,
                        text: 'Sales by Category'
                    },
                    legend: {
                        position: 'bottom'
                    }
                }
            }
        });
    }

    // Food items by category bar chart
    renderFoodItemsByCategoryBar();

    // Sales by hour heatmap
    const foodHourCanvas = document.getElementById('foodHourHeatmap');
    if (foodHourCanvas && data.SalesByHour) {
        const hourData = data.SalesByHour;
        const labels = Array.from({length: 24}, (_, i) => `${i.toString().padStart(2, '0')}:00`);
        
        createResponsiveChart(foodHourCanvas, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Orders by Hour',
                    data: hourData,
                    backgroundColor: hourData.map(value => {
                        const max = Math.max(...hourData);
                        const intensity = value / max;
                        return `rgba(54, 162, 235, ${intensity})`;
                    }),
                    borderColor: '#36A2EB',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: {
                        display: true,
                        text: 'Sales by Hour (24-hour period)'
                    },
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        title: {
                            display: true,
                            text: 'Number of Orders'
                        },
                        ticks: {
                            callback: function(value) {
                                return Number.isInteger(value) ? value : '';
                            },
                            stepSize: 1
                        }
                    },
                    x: {
                        title: {
                            display: true,
                            text: 'Hour of Day'
                        }
                    }
                }
            }
        });
    }
};

// Render food items by category bar chart
function renderFoodItemsByCategoryBar() {
    const items = window.foodAnalyticsData.TopFoodItems || [];
    const categories = ["food", "drink", "combo"];
    // Define color palettes for each category
    const categoryPalettes = {
        food:   ["#8B4513", "#A0522D", "#D2691E", "#CD853F", "#DEB887", "#B87333"], // browns
        drink:  ["#FF8C00", "#FFA500", "#FFB347", "#FFD580", "#FF7F50", "#FF9933"], // oranges
        combo:  ["#FFD700", "#FFFACD", "#FFE066", "#FFEC8B", "#F9E79F", "#F7DC6F"]  // yellows
    };
    // Group food items by category
    const itemsByCategory = {};
    categories.forEach(cat => {
        itemsByCategory[cat] = items.filter(x => x.Category === cat);
    });
    // Assign colors
    const colorMap = {};
    categories.forEach(cat => {
        const palette = categoryPalettes[cat];
        itemsByCategory[cat].forEach((item, idx) => {
            colorMap[item.FoodName] = palette[idx % palette.length];
        });
    });
    // Calculate total revenue per category
    const totals = {};
    categories.forEach(cat => {
        totals[cat] = itemsByCategory[cat].reduce((sum, x) => sum + x.Revenue, 0);
    });
    // Build datasets: one per food item
    const foodNames = items.map(x => x.FoodName);
    const datasets = foodNames.map(food => {
        const foodItem = items.find(x => x.FoodName === food);
        return {
            label: food,
            data: categories.map(cat => {
                if (foodItem && foodItem.Category === cat) {
                    return totals[cat] ? (foodItem.Revenue / totals[cat]) * 100 : 0;
                }
                return 0;
            }),
            backgroundColor: colorMap[food]
        };
    });
    const foodItemsByCategoryBarCanvas = document.getElementById('foodItemsByCategoryBar');
    if (foodItemsByCategoryBarCanvas && foodItemsByCategoryBarCanvas.offsetParent !== null) {
        window.foodItemsByCategoryBarChart = createResponsiveChart(foodItemsByCategoryBarCanvas, {
            type: 'bar',
            data: { labels: categories, datasets },
            options: {
                plugins: {
                    legend: { position: 'bottom' },
                    datalabels: {
                        display: true,
                        color: '#fff',
                        font: { weight: 'bold' },
                        formatter: value => value > 0 ? value.toFixed(2) + '%' : ''
                    }
                },
                scales: {
                    x: { stacked: true },
                    y: {
                        stacked: true,
                        max: 100,
                        beginAtZero: true,
                        ticks: { callback: v => v + '%' }
                    }
                }
            },
            plugins: [ChartDataLabels]
        });
    }
}

// Render top/bottom stacked bar chart
function renderTopBottomStackedBar() {
    const topFoodItems = window.foodAnalyticsData.TopFoodItems || [];
    const { top5, bot5 } = groupTopBottomByType(topFoodItems);
    const foodTypes = ['Food', 'Drink', 'Combo'];
    const datasets = [];
    // Top 5
    foodTypes.forEach((type, idx) => {
        (top5[idx] || []).forEach(item => {
            const data = [0, 0, 0];
            data[idx] = item.Quantity;
            datasets.push({
                label: `★ ${item.FoodName}`,
                data,
                stack: 'TopBottom',
                backgroundColor: 'rgba(40,167,69,0.8)'
            });
        });
    });
    // Bottom 5
    foodTypes.forEach((type, idx) => {
        (bot5[idx] || []).forEach(item => {
            const data = [0, 0, 0];
            data[idx] = item.Quantity;
            datasets.push({
                label: `☆ ${item.FoodName}`,
                data,
                stack: 'TopBottom',
                backgroundColor: 'rgba(40,167,69,0.3)'
            });
        });
    });
    const stackedBarCanvas = document.getElementById('topBottomStackedBar');
    if (stackedBarCanvas) {
        window.topBottomStackedBarObj = createResponsiveChart(stackedBarCanvas, {
            type: 'bar',
            data: { labels: foodTypes, datasets },
            options:{
                responsive:true,
                plugins:{ legend:{ position:'bottom' } },
                scales:{
                    x: { stacked:true },
                    y: {
                        stacked:true,
                        ticks:{
                            callback: value => value + '%'
                        }
                    }
                }
            },
            plugins: [{
                // Convert raw counts to 100%-stacked percentages
                id: 'normalizeTo100',
                beforeInit(chart) {
                    const totalPerType = foodTypes.map((_, i) =>
                        datasets.reduce((sum, ds) => sum + Number(ds.data[i]), 0)
                    );
                    chart.data.datasets.forEach(ds => {
                        ds.data = ds.data.map((v,i) =>
                            totalPerType[i] === 0 ? 0 : (v / totalPerType[i] * 100).toFixed(1)
                        );
                    });
                }
            }]
        });
    }
}

// Tab switching functions
function switchTab(tab) {
    // Remove active class from all buttons
    document.querySelectorAll('.btn-group .btn').forEach(btn => {
        btn.classList.remove('active');
    });

    // Add active class to clicked button
    document.getElementById(tab + '-btn').classList.add('active');

    // Hide all tab panes
    document.querySelectorAll('.tab-pane').forEach(pane => {
        pane.classList.remove('show', 'active');
    });

    // Show selected tab pane
    const selectedPane = document.getElementById(tab + '-list');
    selectedPane.classList.add('show', 'active');
}

function switchMovieTab(tab) {
    document.querySelectorAll('#movie-dashboard .btn-group .btn').forEach(btn => {
        btn.classList.remove('active');
    });
    document.getElementById(tab + '-btn').classList.add('active');

    document.querySelectorAll('#movie-dashboard .tab-pane').forEach(pane => {
        pane.classList.remove('show', 'active');
    });
    document.getElementById(tab + '-list').classList.add('show', 'active');
}

// Download functions
function downloadFoodCharts() {
    const zip = new JSZip();
    const charts = [
        { id: 'foodComboChart', name: 'revenue_orders_trend.png' },
        { id: 'foodCategoryPie', name: 'sales_by_category.png' },
        { id: 'foodItemsByCategoryBar', name: 'food_items_by_category.png' },
        { id: 'foodHourHeatmap', name: 'sales_by_hour.png' }
    ];

    const promises = charts.map(chartInfo => {
        const canvas = document.getElementById(chartInfo.id);
        if (canvas && canvas.chartInstance) {
            return new Promise(resolve => {
                canvas.toBlob(blob => {
                    zip.file(chartInfo.name, blob);
                    resolve();
                });
            });
        }
        return Promise.resolve();
    });

    Promise.all(promises).then(() => {
        zip.generateAsync({ type: "blob" }).then(content => {
            const link = document.createElement('a');
            link.href = URL.createObjectURL(content);
            link.download = 'food_charts.zip';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        });
    });
}

function downloadMovieCharts() {
    const zip = new JSZip();
    const charts = [
        { id: 'movieComboChart', name: 'revenue_vouchers_bookings_trend.png' },
        { id: 'topMoviesChart', name: 'top_movies.png' },
        { id: 'topMembersChart', name: 'top_members.png' }
    ];

    const promises = charts.map(chartInfo => {
        const canvas = document.getElementById(chartInfo.id);
        if (canvas && canvas.chartInstance) {
            return new Promise(resolve => {
                canvas.toBlob(blob => {
                    zip.file(chartInfo.name, blob);
                    resolve();
                });
            });
        }
        return Promise.resolve();
    });

    Promise.all(promises).then(() => {
        zip.generateAsync({ type: "blob" }).then(content => {
            const link = document.createElement('a');
            link.href = URL.createObjectURL(content);
            link.download = 'movie_charts.zip';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        });
    });
}

// Group food items by type for top/bottom analysis
function groupTopBottomByType(topFoodItems) {
    // Group by type: { Food: [...], Drink: [...], Combo: [...] }
    const typeMap = { Food: [], Drink: [], Combo: [] };
    (topFoodItems || []).forEach(item => {
        // Assume food type is in item.FoodName or add a property if available
        // For demo, assign by keyword (customize as needed)
        let type = 'Food';
        const name = item.FoodName.toLowerCase();
        if (name.includes('combo')) type = 'Combo';
        else if (name.includes('water') || name.includes('juice') || name.includes('tea') || name.includes('soda')) type = 'Drink';
        typeMap[type].push(item);
    });
    // Sort by quantity
    Object.keys(typeMap).forEach(type => {
        typeMap[type].sort((a, b) => b.Quantity - a.Quantity);
    });
    // Top 5 and bottom 5 for each type
    const top5 = Object.keys(typeMap).map(type => typeMap[type].slice(0, 5));
    const bot5 = Object.keys(typeMap).map(type => typeMap[type].slice(-5));
    return { top5, bot5 };
}

// Initialize sparkline charts
function initSparklines(revenueData, ordersData) {
    const itemsPerOrderData = ordersData.map((orders, index) => {
        const revenue = revenueData[index];
        return orders > 0 ? revenue / orders : 0;
    });

    const sparklineOptions = {
        type: 'line',
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false },
                tooltip: { enabled: false }
            },
            scales: {
                x: { display: false },
                y: { display: false }
            },
            elements: {
                point: { radius: 0 }
            },
            layout: {
                padding: 0
            }
        }
    };

    createResponsiveChart(document.getElementById('revenue-sparkline'), {
        ...sparklineOptions,
        data: {
            labels: Array(revenueData.length).fill(''),
            datasets: [{
                data: revenueData,
                borderColor: '#28a745',
                borderWidth: 2,
                fill: false
            }]
        }
    });

    createResponsiveChart(document.getElementById('orders-sparkline'), {
        ...sparklineOptions,
        data: {
            labels: Array(ordersData.length).fill(''),
            datasets: [{
                data: ordersData,
                borderColor: '#007bff',
                borderWidth: 2,
                fill: false
            }]
        }
    });

    createResponsiveChart(document.getElementById('items-sparkline'), {
        ...sparklineOptions,
        data: {
            labels: Array(itemsPerOrderData.length).fill(''),
            datasets: [{
                data: itemsPerOrderData,
                borderColor: '#ffc107',
                borderWidth: 2,
                fill: false
            }]
        }
    });
}

// Initialize dashboard on page load
document.addEventListener('DOMContentLoaded', function() {
    showTab('movie');
    // Initialize the movie activity tab as well
    switchMovieTab('booked');
}); 
/**
 * Dashboard Renderer
 * Statik template üzerinde dinamik dashboard oluşturur
 * KPI, Chart, Table ve AI Analysis bileşenlerini render eder
 */
class DashboardRenderer {
    constructor(data, config) {
        this.data = data || [];
        this.config = config || {};
        this.charts = [];
    }

    /**
     * Tüm dashboard bileşenlerini render eder
     */
    async render() {
        console.log('🚀 Dashboard render başlıyor...', {
            dataCount: this.data.length,
            kpiCount: this.config.kpis?.length || 0,
            chartCount: this.config.charts?.length || 0
        });

        try {
            this.applyCustomCss();
            this.renderKPIs();
            this.renderCharts();
            this.renderTable();
            this.renderAnalysis();
            console.log('✅ Dashboard render tamamlandı');
        } catch (error) {
            console.error('❌ Dashboard render hatası:', error);
            throw error;
        }
    }

    /**
     * LLM tarafından üretilen özel CSS stillerini uygular
     */
    applyCustomCss() {
        if (!this.config.customCss) return;
        
        // Mevcut custom style varsa kaldır
        const existingStyle = document.getElementById('dashboard-custom-css');
        if (existingStyle) {
            existingStyle.remove();
        }
        
        // Yeni style elementi oluştur
        const styleElement = document.createElement('style');
        styleElement.id = 'dashboard-custom-css';
        styleElement.textContent = this.config.customCss;
        document.head.appendChild(styleElement);
        
        console.log('🎨 Custom CSS uygulandı:', this.config.customCss.substring(0, 100) + '...');
    }

    // ═══════════════════════════════════════════════════════════════════
    // KPI RENDERING
    // ═══════════════════════════════════════════════════════════════════

    renderKPIs() {
        const container = document.getElementById('kpi-container');
        if (!container || !this.config.kpis) return;

        container.innerHTML = '';
        
        // KPI sayısına göre grid'i dinamik ayarla - her kart eşit genişlikte
        const kpiCount = this.config.kpis.length;
        container.style.display = 'grid';
        container.style.gridTemplateColumns = `repeat(${kpiCount}, 1fr)`;
        container.style.gap = '1rem';
        container.style.marginBottom = '1.5rem';
        
        this.config.kpis.forEach(kpi => {
            const value = this.calculateKPI(kpi);
            const formattedValue = this.formatValue(value, kpi.format);
            const html = this.buildKPICard(kpi, formattedValue);
            container.innerHTML += html;
        });
    }

    calculateKPI(kpi) {
        if (!this.data.length) return 0;

        const values = this.data
            .map(row => this.getColumnValue(row, kpi.column))
            .filter(v => v !== null && v !== undefined);

        const numericValues = values
            .map(v => parseFloat(v))
            .filter(v => !isNaN(v));

        switch (kpi.type) {
            case 'sum':
                return numericValues.reduce((a, b) => a + b, 0);
            case 'avg':
                return numericValues.length > 0 
                    ? numericValues.reduce((a, b) => a + b, 0) / numericValues.length 
                    : 0;
            case 'count':
                return this.data.length;
            case 'min':
                return numericValues.length > 0 ? Math.min(...numericValues) : 0;
            case 'max':
                return numericValues.length > 0 ? Math.max(...numericValues) : 0;
            case 'countDistinct':
                return new Set(values).size;
            default:
                return 0;
        }
    }

    getColumnValue(row, columnName) {
        // Exact match
        if (row[columnName] !== undefined) return row[columnName];
        
        // Case-insensitive match
        const key = Object.keys(row).find(k => 
            k.toLowerCase() === columnName.toLowerCase()
        );
        return key ? row[key] : null;
    }

    formatValue(value, format) {
        if (value === null || value === undefined) return '-';

        switch (format) {
            case 'currency':
                return new Intl.NumberFormat('tr-TR', {
                    style: 'currency',
                    currency: 'TRY',
                    minimumFractionDigits: 0
                }).format(value);
            case 'percent':
                return `%${value.toFixed(1)}`;
            case 'duration':
                if (value < 60) return `${Math.round(value)} dk`;
                const hours = Math.floor(value / 60);
                const mins = Math.round(value % 60);
                return `${hours}sa ${mins}dk`;
            case 'number':
            default:
                return new Intl.NumberFormat('tr-TR', {
                    maximumFractionDigits: 2
                }).format(value);
        }
    }

    buildKPICard(kpi, formattedValue) {
        // Modern, kurumsal ve UI/UX uyumlu düz renkler (gradient yok)
        const colorStyles = {
            blue: {
                bg: 'bg-white',
                border: 'border-l-4 border-l-blue-500',
                title: 'text-slate-600',
                value: 'text-blue-600',
                icon: 'bg-blue-100 text-blue-600'
            },
            green: {
                bg: 'bg-white',
                border: 'border-l-4 border-l-emerald-500',
                title: 'text-slate-600',
                value: 'text-emerald-600',
                icon: 'bg-emerald-100 text-emerald-600'
            },
            red: {
                bg: 'bg-white',
                border: 'border-l-4 border-l-rose-500',
                title: 'text-slate-600',
                value: 'text-rose-600',
                icon: 'bg-rose-100 text-rose-600'
            },
            purple: {
                bg: 'bg-white',
                border: 'border-l-4 border-l-violet-500',
                title: 'text-slate-600',
                value: 'text-violet-600',
                icon: 'bg-violet-100 text-violet-600'
            },
            cyan: {
                bg: 'bg-white',
                border: 'border-l-4 border-l-cyan-500',
                title: 'text-slate-600',
                value: 'text-cyan-600',
                icon: 'bg-cyan-100 text-cyan-600'
            },
            teal: {
                bg: 'bg-white',
                border: 'border-l-4 border-l-teal-500',
                title: 'text-slate-600',
                value: 'text-teal-600',
                icon: 'bg-teal-100 text-teal-600'
            },
            indigo: {
                bg: 'bg-white',
                border: 'border-l-4 border-l-indigo-500',
                title: 'text-slate-600',
                value: 'text-indigo-600',
                icon: 'bg-indigo-100 text-indigo-600'
            },
            pink: {
                bg: 'bg-white',
                border: 'border-l-4 border-l-pink-500',
                title: 'text-slate-600',
                value: 'text-pink-600',
                icon: 'bg-pink-100 text-pink-600'
            }
        };

        const style = colorStyles[kpi.color] || colorStyles.blue;

        return `
            <div class="kpi-card ${style.bg} ${style.border} rounded-xl p-5 shadow-sm hover:shadow-md transition-all duration-200">
                <div class="flex items-center justify-between">
                    <div class="flex-1">
                        <p class="text-sm font-medium ${style.title}">${kpi.title}</p>
                        <p class="text-2xl font-bold mt-1 ${style.value}">${formattedValue}</p>
                    </div>
                    <div class="w-12 h-12 ${style.icon} rounded-xl flex items-center justify-center">
                        <span class="text-2xl">${kpi.icon || '📊'}</span>
                    </div>
                </div>
            </div>
        `;
    }

    // ═══════════════════════════════════════════════════════════════════
    // CHART RENDERING
    // ═══════════════════════════════════════════════════════════════════

    renderCharts() {
        const container = document.getElementById('charts-container');
        if (!container || !this.config.charts) return;

        container.innerHTML = '';

        this.config.charts.forEach(chart => {
            // Chart container oluştur
            const chartDiv = document.createElement('div');
            chartDiv.className = 'bg-white rounded-xl shadow-lg p-5 chart-card w-full';
            chartDiv.innerHTML = `
                <h3 class="text-lg font-semibold text-gray-800 mb-4">${chart.title}</h3>
                <div id="${chart.id}" class="chart-container w-full"></div>
            `;
            container.appendChild(chartDiv);

            // Chart'ı oluştur
            this.buildChart(chart);
        });
    }

    buildChart(chart) {
        const chartData = this.prepareChartData(chart);
        const options = this.getChartOptions(chart, chartData);

        try {
            const apexChart = new ApexCharts(
                document.getElementById(chart.id),
                options
            );
            apexChart.render();
            this.charts.push(apexChart);
        } catch (error) {
            console.error(`Chart render hatası (${chart.id}):`, error);
        }
    }

    prepareChartData(chart) {
        switch (chart.type) {
            case 'bar':
            case 'line':
            case 'area':
                return {
                    categories: this.data.map(row => 
                        this.getColumnValue(row, chart.xAxis) || 'N/A'
                    ),
                    series: [{
                        name: chart.yAxis,
                        data: this.data.map(row => {
                            const val = this.getColumnValue(row, chart.yAxis);
                            return parseFloat(val) || 0;
                        })
                    }]
                };

            case 'pie':
            case 'donut':
                return {
                    labels: this.data.map(row => 
                        this.getColumnValue(row, chart.labelColumn) || 'N/A'
                    ),
                    series: this.data.map(row => {
                        const val = this.getColumnValue(row, chart.valueColumn);
                        return parseFloat(val) || 0;
                    })
                };

            case 'radar':
                return {
                    categories: this.data.map(row => 
                        this.getColumnValue(row, chart.xAxis) || 'N/A'
                    ),
                    series: [{
                        name: chart.yAxis,
                        data: this.data.map(row => {
                            const val = this.getColumnValue(row, chart.yAxis);
                            return parseFloat(val) || 0;
                        })
                    }]
                };

            default:
                return { categories: [], series: [] };
        }
    }

    getChartOptions(chart, chartData) {
        // Modern kurumsal renk paleti - turuncu/sarı tonları hariç
        const defaultColors = [
            '#3B82F6', // Blue
            '#10B981', // Emerald
            '#8B5CF6', // Violet
            '#06B6D4', // Cyan
            '#EC4899', // Pink
            '#6366F1', // Indigo
            '#14B8A6', // Teal
            '#A855F7', // Purple
            '#0EA5E9', // Sky
            '#22C55E'  // Green
        ];
        const colors = chart.colors || defaultColors;

        const baseOptions = {
            chart: {
                type: chart.type === 'donut' ? 'donut' : chart.type,
                height: chart.height || 400,
                width: '100%',
                toolbar: {
                    show: true,
                    tools: {
                        download: true,
                        selection: false,
                        zoom: false,
                        zoomin: false,
                        zoomout: false,
                        pan: false,
                        reset: false
                    }
                },
                fontFamily: 'Inter, system-ui, sans-serif'
            },
            colors: colors,
            responsive: [{
                breakpoint: 480,
                options: {
                    chart: { height: 300 },
                    legend: { position: 'bottom' }
                }
            }],
            title: {
                text: undefined // Başlık zaten HTML'de var
            }
        };

        switch (chart.type) {
            case 'bar':
                return {
                    ...baseOptions,
                    plotOptions: {
                        bar: {
                            horizontal: chart.horizontal || false,
                            borderRadius: 4,
                            columnWidth: '60%'
                        }
                    },
                    dataLabels: { enabled: false },
                    xaxis: {
                        categories: chartData.categories,
                        labels: {
                            style: { fontSize: '12px' },
                            rotate: -45,
                            rotateAlways: chartData.categories.length > 6
                        }
                    },
                    yaxis: {
                        labels: {
                            formatter: (val) => this.formatValue(val, 'number')
                        }
                    },
                    series: chartData.series,
                    tooltip: {
                        y: {
                            formatter: (val) => this.formatValue(val, 'number')
                        }
                    }
                };

            case 'line':
            case 'area':
                return {
                    ...baseOptions,
                    stroke: {
                        curve: chart.smooth ? 'smooth' : 'straight',
                        width: 2
                    },
                    fill: {
                        type: chart.type === 'area' ? 'gradient' : 'solid',
                        gradient: {
                            shadeIntensity: 1,
                            opacityFrom: 0.4,
                            opacityTo: 0.1
                        }
                    },
                    dataLabels: { enabled: false },
                    xaxis: {
                        categories: chartData.categories,
                        labels: { style: { fontSize: '12px' } }
                    },
                    yaxis: {
                        labels: {
                            formatter: (val) => this.formatValue(val, 'number')
                        }
                    },
                    series: chartData.series,
                    tooltip: {
                        y: {
                            formatter: (val) => this.formatValue(val, 'number')
                        }
                    }
                };

            case 'pie':
            case 'donut':
                return {
                    ...baseOptions,
                    labels: chartData.labels,
                    series: chartData.series,
                    legend: {
                        position: 'bottom',
                        fontSize: '12px'
                    },
                    dataLabels: {
                        enabled: true,
                        formatter: (val) => `${val.toFixed(1)}%`
                    },
                    tooltip: {
                        y: {
                            formatter: (val) => this.formatValue(val, 'number')
                        }
                    },
                    plotOptions: {
                        pie: {
                            donut: {
                                size: chart.type === 'donut' ? '60%' : '0%'
                            }
                        }
                    }
                };

            case 'radar':
                return {
                    ...baseOptions,
                    xaxis: {
                        categories: chartData.categories
                    },
                    series: chartData.series,
                    stroke: { width: 2 },
                    fill: { opacity: 0.2 },
                    markers: { size: 4 }
                };

            default:
                return baseOptions;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // TABLE RENDERING
    // ═══════════════════════════════════════════════════════════════════

    /**
     * Kolon adını CamelCase formatına çevirir
     * Orijinal kolon adını data-original-column attribute'unda saklar
     */
    toCamelCase(str) {
        if (!str) return str;
        return str
            .split(' ')
            .map(word => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
            .join(' ');
    }

    /**
     * Kolonun select filtre için uygun olup olmadığını kontrol eder
     * Benzersiz değer sayısı 2-20 arası ise select için uygundur
     */
    isSelectableColumn(columnName) {
        const uniqueValues = [...new Set(this.data.map(row => this.getColumnValue(row, columnName)))];
        const validValues = uniqueValues.filter(v => v !== null && v !== undefined && v !== '');
        return validValues.length >= 2 && validValues.length <= 20;
    }

    /**
     * Kolon için benzersiz değerleri döndürür
     */
    getUniqueColumnValues(columnName) {
        const uniqueValues = [...new Set(this.data.map(row => this.getColumnValue(row, columnName)))];
        return uniqueValues
            .filter(v => v !== null && v !== undefined && v !== '')
            .sort((a, b) => String(a).localeCompare(String(b), 'tr'));
    }

    renderTable() {
        const table = document.getElementById('data-table');
        if (!table || !this.data.length) return;

        const tableConfig = this.config.table || {};
        const columns = tableConfig.columns || Object.keys(this.data[0]);

        // Filtrelenebilir kolonları belirle
        const selectableColumns = columns.filter(col => this.isSelectableColumn(col));

        // Filtre container oluştur
        if (selectableColumns.length > 0) {
            this.renderTableFilters(table, columns, selectableColumns);
        }

        // Thead oluştur - CamelCase başlıklar, orijinal kolon adı data attribute'da
        const thead = document.createElement('thead');
        thead.innerHTML = `
            <tr>
                ${columns.map(col => `<th data-original-column="${col}">${this.toCamelCase(col)}</th>`).join('')}
            </tr>
        `;
        table.appendChild(thead);

        // Tbody oluştur
        const tbody = document.createElement('tbody');
        this.data.forEach(row => {
            const tr = document.createElement('tr');
            tr.innerHTML = columns.map(col => {
                const value = this.getColumnValue(row, col);
                return `<td data-original-column="${col}">${value !== null && value !== undefined ? value : '-'}</td>`;
            }).join('');
            tbody.appendChild(tr);
        });
        table.appendChild(tbody);

        // DataTables başlat
        const dataTable = $(table).DataTable({
            pageLength: tableConfig.pageSize || 10,
            order: tableConfig.sortBy 
                ? [[columns.indexOf(tableConfig.sortBy), tableConfig.sortOrder || 'desc']]
                : [],
            language: {
                url: './js/tr.json'
            },
            dom: 'Brtip', // 'f' (search) kaldırıldı - select filtreler kullanılacak
            buttons: [
                {
                    extend: 'excelHtml5',
                    text: '<img src="./assets/excel.png" style="width:20px;height:20px;">',
                    className: 'btn-export',
                    title: 'Rapor'
                },
                {
                    extend: 'pdfHtml5',
                    text: '<img src="./assets/pdf.png" style="width:20px;height:20px;">',
                    className: 'btn-export',
                    title: 'Rapor'
                }
            ],
            responsive: true
        });

        // Select filtreleri DataTable'a bağla
        if (selectableColumns.length > 0) {
            this.bindTableFilters(dataTable, columns);
        }

        // Parent div'e modern-table-wrapper class'ı ekle
        const parentDiv = table.parentElement;
        if (parentDiv) {
            parentDiv.classList.add('modern-table-wrapper');
        }
    }

    /**
     * Tablo üstüne select filtreler ekler
     */
    renderTableFilters(table, columns, selectableColumns) {
        const filterContainer = document.createElement('div');
        filterContainer.id = 'table-filters';
        filterContainer.className = 'flex flex-wrap gap-3 mb-4 p-4 bg-slate-50 rounded-xl border border-slate-200';

        selectableColumns.forEach(col => {
            const uniqueValues = this.getUniqueColumnValues(col);
            const colIndex = columns.indexOf(col);
            
            const filterGroup = document.createElement('div');
            filterGroup.className = 'flex flex-col gap-1';
            filterGroup.innerHTML = `
                <label class="text-xs font-medium text-slate-500">${this.toCamelCase(col)}</label>
                <select class="table-filter px-3 py-2 text-sm border border-slate-300 rounded-lg bg-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 min-w-[150px]" 
                        data-column-index="${colIndex}">
                    <option value="">Tümü</option>
                    ${uniqueValues.map(val => `<option value="${val}">${val}</option>`).join('')}
                </select>
            `;
            filterContainer.appendChild(filterGroup);
        });

        // Filtreleri temizle butonu
        const clearBtn = document.createElement('div');
        clearBtn.className = 'flex items-end';
        clearBtn.innerHTML = `
            <button id="clear-filters" class="px-4 py-2 text-sm bg-slate-200 hover:bg-slate-300 text-slate-700 rounded-lg transition-colors">
                🔄 Temizle
            </button>
        `;
        filterContainer.appendChild(clearBtn);

        // Tablodan önce ekle
        table.parentElement.insertBefore(filterContainer, table);
    }

    /**
     * Select filtreleri DataTable'a bağlar
     */
    bindTableFilters(dataTable, columns) {
        // Her select için change event
        document.querySelectorAll('.table-filter').forEach(select => {
            select.addEventListener('change', () => {
                const colIndex = parseInt(select.dataset.columnIndex);
                const value = select.value;
                
                // DataTable kolonunu filtrele
                dataTable.column(colIndex).search(value ? `^${this.escapeRegex(value)}$` : '', true, false).draw();
            });
        });

        // Temizle butonu
        document.getElementById('clear-filters')?.addEventListener('click', () => {
            document.querySelectorAll('.table-filter').forEach(select => {
                select.value = '';
            });
            dataTable.columns().search('').draw();
        });
    }

    /**
     * Regex özel karakterlerini escape eder
     */
    escapeRegex(str) {
        return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    }

    // ═══════════════════════════════════════════════════════════════════
    // AI ANALYSIS RENDERING
    // ═══════════════════════════════════════════════════════════════════

    renderAnalysis() {
        const container = document.getElementById('analysis-content');
        if (!container || !this.config.analysis) return;

        const analysis = this.config.analysis;

        container.innerHTML = `
            ${this.renderExecutiveSummary(analysis.executiveSummary)}
            
            <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
                
                <!-- Sol Kolon -->
                <div class="space-y-6">
                    
                    <!-- 1. 📈 Genel Özet (Executive Summary) -->
                    <div class="analysis-card bg-white rounded-xl p-5 border border-slate-200 shadow-sm">
                        <div class="flex items-center gap-2 mb-4">
                            <span class="w-8 h-8 bg-blue-100 rounded-lg flex items-center justify-center text-lg">📈</span>
                            <h4 class="text-base font-semibold text-slate-800">Genel Özet</h4>
                        </div>
                        <p class="text-sm text-slate-600 mb-4">${analysis.summary || 'Özet bilgisi mevcut değil.'}</p>
                        
                        ${analysis.statistics ? `
                            <div class="grid grid-cols-3 gap-3">
                                <div class="bg-slate-50 rounded-lg p-3 text-center border border-slate-100">
                                    <span class="text-2xl font-bold text-blue-600">${this.formatValue(analysis.statistics.total, 'number')}</span>
                                    <p class="text-xs text-slate-500 mt-1">Toplam</p>
                                </div>
                                <div class="bg-slate-50 rounded-lg p-3 text-center border border-slate-100">
                                    <span class="text-2xl font-bold text-emerald-600">${this.formatValue(analysis.statistics.average, 'number')}</span>
                                    <p class="text-xs text-slate-500 mt-1">Ortalama</p>
                                </div>
                                <div class="bg-slate-50 rounded-lg p-3 text-center border border-slate-100">
                                    <span class="text-2xl font-bold text-purple-600">${this.formatValue(analysis.statistics.median, 'number')}</span>
                                    <p class="text-xs text-slate-500 mt-1">Medyan</p>
                                </div>
                            </div>
                        ` : ''}
                    </div>

                    <!-- 2. 🏆 Öne Çıkanlar (Top Performers) -->
                    ${analysis.highlights?.length ? `
                        <div class="analysis-card bg-white rounded-xl p-5 border-l-4 border-l-emerald-500 border-t border-r border-b border-slate-200 shadow-sm">
                            <div class="flex items-center gap-2 mb-4">
                                <span class="w-8 h-8 bg-emerald-100 rounded-lg flex items-center justify-center text-lg">🏆</span>
                                <h4 class="text-base font-semibold text-emerald-600">Öne Çıkanlar</h4>
                            </div>
                            <div class="space-y-2">
                                ${analysis.highlights.map((h, i) => `
                                    <div class="flex items-center gap-3 bg-white rounded-lg p-3 border border-slate-100">
                                        <span class="w-8 h-8 ${this.getHighlightColor(i)} rounded-full flex items-center justify-center text-white font-bold text-sm shadow-sm">${i + 1}</span>
                                        <div class="flex-1 min-w-0">
                                            <p class="font-medium text-slate-800 text-sm">${h.title || ''}</p>
                                            <p class="text-xs text-emerald-600">${h.text}${h.value ? ` - ${h.value}` : ''}</p>
                                        </div>
                                    </div>
                                `).join('')}
                            </div>
                        </div>
                    ` : ''}

                    <!-- 3. 📉 Düşük Performans (Needs Attention) -->
                    ${analysis.lowPerformers?.length ? `
                        <div class="analysis-card bg-white rounded-xl p-5 border-l-4 border-l-amber-400 border-t border-r border-b border-slate-200 shadow-sm">
                            <div class="flex items-center gap-2 mb-4">
                                <span class="w-8 h-8 bg-amber-100 rounded-lg flex items-center justify-center text-lg">📉</span>
                                <h4 class="text-base font-semibold text-amber-600">Düşük Performans</h4>
                            </div>
                            <div class="space-y-2">
                                ${analysis.lowPerformers.map(item => `
                                    <div class="flex items-center gap-3 bg-white rounded-lg p-3 border border-slate-100">
                                        <span class="w-2.5 h-2.5 ${item.critical ? 'bg-red-500' : 'bg-orange-400'} rounded-full flex-shrink-0"></span>
                                        <div class="flex-1 min-w-0">
                                            <p class="text-sm text-slate-700"><strong class="text-slate-800">${item.title}:</strong> ${item.value}</p>
                                            <p class="text-xs text-rose-500">${item.description || ''}</p>
                                        </div>
                                    </div>
                                `).join('')}
                            </div>
                        </div>
                    ` : ''}

                    <!-- 4. 📊 İstatistiksel Özet -->
                    ${analysis.detailedStats ? `
                        <div class="analysis-card bg-white rounded-xl p-5 border border-slate-200 shadow-sm">
                            <div class="flex items-center gap-2 mb-4">
                                <span class="w-8 h-8 bg-slate-100 rounded-lg flex items-center justify-center text-lg">📊</span>
                                <h4 class="text-base font-semibold text-slate-800">İstatistiksel Özet</h4>
                            </div>
                            <div class="grid grid-cols-2 gap-2">
                                <div class="flex justify-between items-center bg-slate-50 rounded-lg px-3 py-2 border border-slate-100">
                                    <span class="text-xs text-slate-500">Minimum</span>
                                    <span class="text-sm font-semibold text-slate-800">${this.formatValue(analysis.detailedStats.min, 'number')}</span>
                                </div>
                                <div class="flex justify-between items-center bg-slate-50 rounded-lg px-3 py-2 border border-slate-100">
                                    <span class="text-xs text-slate-500">Maksimum</span>
                                    <span class="text-sm font-semibold text-slate-800">${this.formatValue(analysis.detailedStats.max, 'number')}</span>
                                </div>
                                <div class="flex justify-between items-center bg-slate-50 rounded-lg px-3 py-2 border border-slate-100">
                                    <span class="text-xs text-slate-500">Ortalama</span>
                                    <span class="text-sm font-semibold text-slate-800">${this.formatValue(analysis.detailedStats.average, 'number')}</span>
                                </div>
                                <div class="flex justify-between items-center bg-slate-50 rounded-lg px-3 py-2 border border-slate-100">
                                    <span class="text-xs text-slate-500">Medyan</span>
                                    <span class="text-sm font-semibold text-slate-800">${this.formatValue(analysis.detailedStats.median, 'number')}</span>
                                </div>
                                <div class="flex justify-between items-center bg-slate-50 rounded-lg px-3 py-2 border border-slate-100">
                                    <span class="text-xs text-slate-500">Std. Sapma</span>
                                    <span class="text-sm font-semibold text-slate-800">${this.formatValue(analysis.detailedStats.stdDev, 'number')}</span>
                                </div>
                                <div class="flex justify-between items-center bg-slate-50 rounded-lg px-3 py-2 border border-slate-100">
                                    <span class="text-xs text-slate-500">Toplam</span>
                                    <span class="text-sm font-semibold text-slate-800">${this.formatValue(analysis.detailedStats.sum, 'number')}</span>
                                </div>
                            </div>
                        </div>
                    ` : ''}

                    <!-- 9. 🔮 Öngörüler (Predictions) -->
                    ${analysis.predictions?.length ? `
                        <div class="analysis-card bg-white rounded-xl p-5 border-l-4 border-l-fuchsia-500 border-t border-r border-b border-slate-200 shadow-sm">
                            <div class="flex items-center gap-2 mb-4">
                                <span class="w-8 h-8 bg-fuchsia-100 rounded-lg flex items-center justify-center text-lg">🔮</span>
                                <h4 class="text-base font-semibold text-fuchsia-600">Öngörüler</h4>
                            </div>
                            <div class="space-y-2">
                                ${analysis.predictions.map(p => `
                                    <div class="flex items-start gap-2 bg-white rounded-lg p-3 border border-slate-100">
                                        <span class="text-base">${p.icon || '📅'}</span>
                                        <div class="flex-1 min-w-0">
                                            <p class="text-xs font-medium text-fuchsia-600">${p.title}</p>
                                            <p class="text-sm text-slate-700">${p.text}</p>
                                        </div>
                                    </div>
                                `).join('')}
                            </div>
                        </div>
                    ` : ''}
                </div>

                <!-- Sağ Kolon -->
                <div class="space-y-6">
                    
                    <!-- 5. 🔍 Dağılım Analizi -->
                    ${analysis.distribution?.length ? `
                        <div class="analysis-card bg-white rounded-xl p-5 border-l-4 border-l-violet-500 border-t border-r border-b border-slate-200 shadow-sm">
                            <div class="flex items-center gap-2 mb-4">
                                <span class="w-8 h-8 bg-violet-100 rounded-lg flex items-center justify-center text-lg">🔍</span>
                                <h4 class="text-base font-semibold text-violet-600">Dağılım Analizi</h4>
                            </div>
                            <div class="space-y-3">
                                <p class="text-xs font-medium text-slate-500 mb-2">Kanal Dağılımı</p>
                                <div class="space-y-1.5">
                                    ${analysis.distribution.map((d, i) => `
                                        <div class="flex items-center gap-2">
                                            <div class="flex-1 bg-slate-100 rounded-full h-2 overflow-hidden">
                                                <div class="${this.getDistributionBarColor(i)} h-full rounded-full" style="width: ${d.percent}%"></div>
                                            </div>
                                            <span class="text-xs text-slate-600 w-32 text-right">${d.category}: %${d.percent.toFixed(0)}</span>
                                        </div>
                                    `).join('')}
                                </div>
                                ${analysis.distributionNotes ? `
                                    <div class="pt-3 mt-3 border-t border-slate-100 space-y-1">
                                        ${analysis.distributionNotes.concentration ? `<p class="text-sm text-slate-700"><strong class="text-slate-800">Yoğunlaşma:</strong> ${analysis.distributionNotes.concentration}</p>` : ''}
                                        ${analysis.distributionNotes.imbalance ? `<p class="text-sm text-slate-700"><strong class="text-slate-800">Dengesizlik:</strong> ${analysis.distributionNotes.imbalance}</p>` : ''}
                                    </div>
                                ` : ''}
                            </div>
                        </div>
                    ` : ''}

                    <!-- 6. 📈 Trend Analizi -->
                    ${analysis.trend ? `
                        <div class="analysis-card bg-white rounded-xl p-5 border-l-4 border-l-blue-500 border-t border-r border-b border-slate-200 shadow-sm">
                            <div class="flex items-center gap-2 mb-4">
                                <span class="w-8 h-8 bg-blue-100 rounded-lg flex items-center justify-center text-lg">📈</span>
                                <h4 class="text-base font-semibold text-blue-600">Trend Analizi</h4>
                            </div>
                            <div class="grid grid-cols-2 gap-3">
                                <div class="bg-slate-50 rounded-lg p-3 border border-slate-100">
                                    <p class="text-xs text-slate-500 mb-1">Genel Trend</p>
                                    <p class="text-lg font-bold ${this.getTrendColor(analysis.trend.direction)}">${this.getTrendIcon(analysis.trend.direction)} ${this.getTrendLabel(analysis.trend.direction)}</p>
                                </div>
                                <div class="bg-slate-50 rounded-lg p-3 border border-slate-100">
                                    <p class="text-xs text-slate-500 mb-1">Değişim Oranı</p>
                                    <p class="text-lg font-bold text-indigo-600">%${analysis.trend.changeRate || 0}</p>
                                </div>
                                <div class="bg-slate-50 rounded-lg p-3 border border-slate-100">
                                    <p class="text-xs text-slate-500 mb-1">Zirve Noktası</p>
                                    <p class="text-sm font-semibold text-slate-800">${analysis.trend.peak || '-'}</p>
                                </div>
                                <div class="bg-slate-50 rounded-lg p-3 border border-slate-100">
                                    <p class="text-xs text-slate-500 mb-1">Dip Noktası</p>
                                    <p class="text-sm font-semibold text-slate-800">${analysis.trend.low || '-'}</p>
                                </div>
                            </div>
                        </div>
                    ` : ''}

                    <!-- 7. ⚠️ Dikkat Edilmesi Gerekenler -->
                    ${analysis.insights?.length ? `
                        <div class="analysis-card bg-white rounded-xl p-5 border-l-4 border-l-rose-500 border-t border-r border-b border-slate-200 shadow-sm">
                            <div class="flex items-center gap-2 mb-4">
                                <span class="w-8 h-8 bg-rose-100 rounded-lg flex items-center justify-center text-lg">⚠️</span>
                                <h4 class="text-base font-semibold text-rose-600">Dikkat Edilmesi Gerekenler</h4>
                            </div>
                            <div class="space-y-2">
                                ${analysis.insights.map(i => `
                                    <div class="flex items-start gap-2 bg-white rounded-lg p-3 border border-slate-100">
                                        <span class="px-2 py-0.5 ${this.getInsightBadgeClass(i.type)} text-xs font-bold rounded flex-shrink-0">
                                            ${this.getInsightLabel(i.type)}
                                        </span>
                                        <p class="text-sm text-slate-700">${i.text}</p>
                                    </div>
                                `).join('')}
                            </div>
                        </div>
                    ` : ''}

                    <!-- 8. 💡 Önerilen Aksiyonlar -->
                    ${analysis.recommendations?.length ? `
                        <div class="analysis-card bg-white rounded-xl p-5 border-l-4 border-l-yellow-500 border-t border-r border-b border-slate-200 shadow-sm">
                            <div class="flex items-center gap-2 mb-4">
                                <span class="w-8 h-8 bg-yellow-100 rounded-lg flex items-center justify-center text-lg">💡</span>
                                <h4 class="text-base font-semibold text-yellow-600">Önerilen Aksiyonlar</h4>
                            </div>
                            <div class="space-y-2">
                                ${analysis.recommendations.map(r => `
                                    <div class="bg-white rounded-lg p-3 border border-slate-100">
                                        <div class="flex items-start gap-2">
                                            <span class="px-2 py-0.5 ${this.getPriorityBadgeClass(r.priority)} text-xs font-bold rounded flex-shrink-0">
                                                ${this.getPriorityLabel(r.priority)}
                                            </span>
                                            <div>
                                                <p class="font-medium text-slate-800 text-sm">${r.title}</p>
                                                <p class="text-xs text-slate-500 mt-0.5">${r.text}</p>
                                            </div>
                                        </div>
                                    </div>
                                `).join('')}
                            </div>
                        </div>
                    ` : ''}
                </div>
            </div>
        `;
    }

    getDistributionBarColor(index) {
        const colors = [
            'bg-emerald-500',
            'bg-blue-500',
            'bg-violet-500',
            'bg-cyan-500',
            'bg-teal-500',
            'bg-indigo-500'
        ];
        return colors[index % colors.length];
    }

    getTrendColor(direction) {
        const colors = {
            up: 'text-emerald-600',
            down: 'text-red-600',
            stable: 'text-slate-600'
        };
        return colors[direction] || colors.stable;
    }

    getTrendIcon(direction) {
        const icons = {
            up: '↑',
            down: '↓',
            stable: '→'
        };
        return icons[direction] || icons.stable;
    }

    getTrendLabel(direction) {
        const labels = {
            up: 'Artış',
            down: 'Azalış',
            stable: 'Sabit'
        };
        return labels[direction] || labels.stable;
    }

    getHighlightColor(index) {
        // Sıralamaya göre altın, gümüş, bronz renkleri
        const colors = [
            'bg-yellow-400',  // 1. - Altın
            'bg-gray-400',    // 2. - Gümüş
            'bg-amber-600',   // 3. - Bronz
            'bg-emerald-500', // 4+
            'bg-teal-500',
            'bg-cyan-500'
        ];
        return colors[index] || 'bg-emerald-500';
    }

    getInsightBadgeClass(type) {
        const classes = {
            critical: 'bg-red-500 text-white',
            warning: 'bg-amber-500 text-white',
            trend: 'bg-blue-500 text-white',
            success: 'bg-green-500 text-white',
            info: 'bg-slate-500 text-white'
        };
        return classes[type] || classes.info;
    }

    getInsightLabel(type) {
        const labels = {
            critical: 'KRİTİK',
            warning: 'UYARI',
            trend: 'TREND',
            success: 'BAŞARI',
            info: 'BİLGİ'
        };
        return labels[type] || 'BİLGİ';
    }

    getPriorityBadgeClass(priority) {
        const classes = {
            high: 'bg-red-100 text-red-700',
            medium: 'bg-emerald-100 text-emerald-700',
            low: 'bg-slate-100 text-slate-700'
        };
        return classes[priority] || classes.medium;
    }

    getPriorityLabel(priority) {
        const labels = {
            high: 'ACİL',
            medium: 'KISA VADE',
            low: 'UZUN VADE'
        };
        return labels[priority] || 'ORTA';
    }

    // ═══════════════════════════════════════════════════════════════════
    // EXECUTIVE SUMMARY RENDERING
    // ═══════════════════════════════════════════════════════════════════

    renderExecutiveSummary(executiveSummary) {
        if (!executiveSummary) return '';

        return `
            <div class="executive-summary bg-indigo-600 rounded-2xl p-6 mb-6 text-white shadow-xl">
                <div class="flex items-center gap-3 mb-4">
                    <span class="w-10 h-10 bg-white/20 rounded-xl flex items-center justify-center text-2xl">📋</span>
                    <div>
                        <h3 class="text-xl font-bold">${executiveSummary.title || 'Yönetici Özeti'}</h3>
                        <p class="text-indigo-200 text-sm">Üst Yönetim için Hazırlanmıştır</p>
                    </div>
                </div>
                
                <!-- Overview -->
                <div class="bg-white/10 rounded-xl p-4 mb-4 backdrop-blur-sm">
                    <p class="text-white/95 leading-relaxed">${executiveSummary.overview || ''}</p>
                </div>
                
                <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <!-- Key Findings -->
                    ${executiveSummary.keyFindings?.length ? `
                        <div class="bg-white/10 rounded-xl p-4 backdrop-blur-sm">
                            <div class="flex items-center gap-2 mb-3">
                                <span class="text-lg">🔍</span>
                                <h4 class="font-semibold text-white">Ana Bulgular</h4>
                            </div>
                            <ul class="space-y-2">
                                ${executiveSummary.keyFindings.map(finding => `
                                    <li class="flex items-start gap-2 text-sm text-white/90">
                                        <span class="text-green-300 mt-0.5">✓</span>
                                        <span>${finding}</span>
                                    </li>
                                `).join('')}
                            </ul>
                        </div>
                    ` : ''}
                    
                    <!-- Action Items -->
                    ${executiveSummary.actionItems?.length ? `
                        <div class="bg-white/10 rounded-xl p-4 backdrop-blur-sm">
                            <div class="flex items-center gap-2 mb-3">
                                <span class="text-lg">⚡</span>
                                <h4 class="font-semibold text-white">Aksiyon Gerektiren Maddeler</h4>
                            </div>
                            <ul class="space-y-2">
                                ${executiveSummary.actionItems.map((item, index) => `
                                    <li class="flex items-start gap-2 text-sm text-white/90">
                                        <span class="w-5 h-5 bg-cyan-400 rounded-full flex items-center justify-center text-xs font-bold text-white flex-shrink-0">${index + 1}</span>
                                        <span>${item}</span>
                                    </li>
                                `).join('')}
                            </ul>
                        </div>
                    ` : ''}
                </div>
                
                <!-- Conclusion -->
                ${executiveSummary.conclusion ? `
                    <div class="mt-4 pt-4 border-t border-white/20">
                        <div class="flex items-center gap-2">
                            <span class="text-lg">💡</span>
                            <p class="text-white/95 font-medium">${executiveSummary.conclusion}</p>
                        </div>
                    </div>
                ` : ''}
            </div>
        `;
    }
}

// Global export
window.DashboardRenderer = DashboardRenderer;

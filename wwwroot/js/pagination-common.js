// Common Pagination Functions
class PaginationManager {
    constructor(options) {
        this.currentPage = 1;
        this.pageSize = options.pageSize || 10;
        this.containerId = options.containerId;
        this.resultId = options.resultId;
        this.paginationId = options.paginationId;
        this.statisticsIds = options.statisticsIds || {};
        this.loadFunction = options.loadFunction;
        this.renderFunction = options.renderFunction;
        this.updateStatsFunction = options.updateStatsFunction;
    }

    // Load data with pagination
    loadData(params = {}, page = 1) {
        
        const loadingButton = $(`#${this.containerId} button[type="submit"]`);
        if (loadingButton.length) {
            loadingButton.prop('disabled', true);
            loadingButton.html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Loading...');
        }
        
        $(`#${this.paginationId}`).addClass('loading');
        
        const requestData = {
            ...params,
            page: page,
            pageSize: this.pageSize
        };

        $.ajax({
            url: this.loadFunction.url,
            type: 'GET',
            data: requestData,
            success: (response) => {
                if (response.success) {
                    this.currentPage = page;
                    this.renderFunction(response.data);
                    this.renderPagination(response.pagination);
                    if (this.updateStatsFunction) {
                        this.updateStatsFunction(response.statistics);
                    }
                    
                    if (page > 1) {
                        $(`#${this.resultId}`).get(0).scrollIntoView({ 
                            behavior: 'smooth', 
                            block: 'start' 
                        });
                    }
                } else {
                    $(`#${this.resultId}`).html(`<div class="alert alert-danger"><i class="fas fa-exclamation-triangle"></i> ${response.message}</div>`);
                }
            },
            error: (xhr, status, error) => {
                console.error('AJAX error:', status, error);
                console.error('Response:', xhr.responseText);
                $(`#${this.resultId}`).html('<div class="alert alert-danger"><i class="fas fa-exclamation-triangle"></i> Error loading data. Please check console for details.</div>');
            },
            complete: () => {
                if (loadingButton.length) {
                    loadingButton.prop('disabled', false);
                    loadingButton.html('<i class="bi bi-search"></i> Search');
                }
                $(`#${this.paginationId}`).removeClass('loading');
            }
        });
    }

    // Render pagination controls
    renderPagination(pagination) {
        const totalPages = pagination.totalPages;
        if (totalPages <= 1) {
            $(`#${this.paginationId}`).html('');
            return;
        }
        
        const maxVisiblePages = 7;
        let startPage = Math.max(1, this.currentPage - Math.floor(maxVisiblePages / 2));
        let endPage = Math.min(totalPages, startPage + maxVisiblePages - 1);
        
        if (endPage - startPage + 1 < maxVisiblePages) {
            startPage = Math.max(1, endPage - maxVisiblePages + 1);
        }
        
        let html = '<div class="pagination-container">';
        html += '<ul class="pagination justify-content-center">';
        
        // Previous button
        html += `<li class="page-item${this.currentPage === 1 ? ' disabled' : ''}">`;
        html += `<a class="page-link" href="#" data-page="${this.currentPage - 1}"><i class="fas fa-chevron-left"></i></a>`;
        html += '</li>';
        
        // First page + ellipsis
        if (startPage > 1) {
            html += '<li class="page-item"><a class="page-link" href="#" data-page="1">1</a></li>';
            if (startPage > 2) {
                html += '<li class="page-item disabled"><span class="page-link">...</span></li>';
            }
        }
        
        // Visible pages
        for (let i = startPage; i <= endPage; i++) {
            html += `<li class="page-item${this.currentPage === i ? ' active' : ''}">`;
            html += `<a class="page-link" href="#" data-page="${i}">${i}</a>`;
            html += '</li>';
        }
        
        // Last page + ellipsis
        if (endPage < totalPages) {
            if (endPage < totalPages - 1) {
                html += '<li class="page-item disabled"><span class="page-link">...</span></li>';
            }
            html += `<li class="page-item"><a class="page-link" href="#" data-page="${totalPages}">${totalPages}</a></li>`;
        }
        
        // Next button
        html += `<li class="page-item${this.currentPage === totalPages ? ' disabled' : ''}">`;
        html += `<a class="page-link" href="#" data-page="${this.currentPage + 1}"><i class="fas fa-chevron-right"></i></a>`;
        html += '</li>';
        
        html += '</ul>';
        html += '</div>';
        
        $(`#${this.paginationId}`).html(html);
        this.bindPaginationEvents(totalPages);
    }

    // Bind pagination click events
    bindPaginationEvents(totalPages) {
        $(`#${this.paginationId} .page-link`).off('click').on('click', (e) => {
            e.preventDefault();
            const page = parseInt($(e.currentTarget).data('page'));
            if (!isNaN(page) && page >= 1 && page <= totalPages && page !== this.currentPage) {
                this.loadData(this.getCurrentParams(), page);
            }
        });
    }

    // Get current form parameters
    getCurrentParams() {
        const params = {};
        $(`#${this.containerId} input, #${this.containerId} select`).each(function() {
            const $this = $(this);
            const name = $this.attr('name');
            if (name) {
                if ($this.attr('type') === 'radio') {
                    if ($this.is(':checked')) {
                        params[name] = $this.val();
                    }
                } else {
                    params[name] = $this.val();
                }
            }
        });
        return params;
    }

    // Initialize pagination
    init() {
        console.log('Initializing pagination for:', this.containerId);
        
        // Handle form submit
        $(`#${this.containerId}`).on('submit', (e) => {
            e.preventDefault();
            console.log('Form submitted, loading data...');
            this.loadData(this.getCurrentParams(), 1);
        });

        // Load initial data
        console.log('Loading initial data...');
        this.loadData({}, 1);
    }
}

// Utility function to create pagination manager
function createPaginationManager(options) {
    console.log('Creating pagination manager with options:', options);
    const manager = new PaginationManager(options);
    manager.init();
    return manager;
} 
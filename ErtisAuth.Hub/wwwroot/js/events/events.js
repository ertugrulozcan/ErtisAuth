"use strict";

function initTable(tableId, apiEndpoint) {
    let columnDefinitions = [
        {
            targets: 0,
            searchable: false,
            orderable: false,
            visible: true,
            render: function(data, type, full, meta) {
                let event = convertToEvent(full);
                let css = getEventColorClass(event.event_type);
                let icon = getEventIconClass(event.event_type);
                
                return `` +
                    `<div class="symbol symbol-40px" style="border: 1px solid #e8e8e8;"><div class="symbol-label"><i class="bi bi-` + icon + ` text-` + css + ` fs-2" style="padding-top: 3px;"></i></div></div>`;
            }
        },
        {
            targets: 1,
            title: 'Id',
            searchable: false,
            orderable: true,
            orderData: [ 0 ],
            visible: true,
            render: function(data, type, full, meta) {
                let event = convertToEvent(full);
                return `` +
                    `<a href="/events/` + event.id + `" style="color: #333333;">` + event.id + `</a>`;
            }
        },
        {
            targets: 2,
            title: 'Source',
            searchable: false,
            orderable: false,
            orderData: [ 1 ],
            render: function(data, type, full, meta) {
                let event = convertToEvent(full);
                if (event.utilizer_type === 'User') {
                    return `` +
                        `<div class="badge badge-light-primary fw-bolder">` + event.utilizer_type + `</div>`;
                }
                else if (event.utilizer_type === 'User') {
                    return `` +
                        `<div class="badge badge-light-info fw-bolder">` + event.utilizer_type + `</div>`;
                }
                else {
                    return `` +
                        `<div class="badge badge-light-secondary fw-bolder">` + event.utilizer_type + `</div>`;   
                }
            }
        },
        {
            targets: 3,
            title: 'Utilizer',
            searchable: true,
            orderable: true,
            orderData: [ 2 ],
            render: function(data, type, full, meta) {
                let event = convertToEvent(full);
                if (event.utilizer_name && !stringIsNullOrEmpty(event.utilizer_name) || event.utilizer_id && !stringIsNullOrEmpty(event.utilizer_id)) {
                    return `` +
                        `<div style="display: flex; flex-direction: column;">` +
                        `<span class="text-gray-700">` + event.utilizer_name + `</span>` +
                        `<span class="text-gray-400">` + event.utilizer_id + `</span>` +
                        `</div>`;
                }
                else {
                    return `` +
                        `<span class="text-gray-400">Unknown</span>`;
                }
            }
        },
        {
            targets: 4,
            title: 'Event Type',
            searchable: false,
            orderable: true,
            orderData: [ 4 ],
            render: function(data, type, full, meta) {
                let event = convertToEvent(full);
                return event.event_type;
            }
        },
        {
            targets: 5,
            title: 'Event Time',
            searchable: false,
            orderable: true,
            orderData: [ 5 ],
            render: function(data, type, full, meta) {
                let event = convertToEvent(full);
                return event.event_time;
            }
        },
        {
            targets: -1,
            title: '',
            searchable: false,
            orderable: false,
            render: function(data, type, full, meta) {
                let event = convertToEvent(full);
                let dropdownHtml = getDatatableDropdown('/events/' + event.id, 'event', event.id, null, false);
                return dropdownHtml;
            }
        }
    ];

    let table = $('#' + tableId);

    let dataTable = table.DataTable({
        pagingType: 'full_numbers',
        serverSide: true,
        paging: true,
        searching: true,
        processing: true,
        orderMulti: false,
        stateSave: false,
        responsive: false,
        lengthMenu: [ 10, 25, 50, 75, 100 ],
        displayStart: 0,
        order: [[5, 'asc']],
        searchDelay: 600,
        columnDefs: columnDefinitions,
        ajax: {
            url: apiEndpoint,
            type: 'GET'
        }
    });
}

let page = function() {
    return {
        init: function () {
            initTable('events_data_table', '/api/events');
        }
    }
}();

KTUtil.onDOMContentLoaded((function() {
    page.init();
}));
"use strict";

function initTable(tableId, apiEndpoint) {
    let columnDefinitions = [
        {
            targets: 0,
            title: '',
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
            orderData: [ 1 ],
            visible: true,
            render: function(data, type, full, meta) {
                let event = convertToEvent(full);
                return `` +
                    `<a href="/events/` + event.id + `" style="color: #333333;">` + event.id + `</a>`;
            }
        },
        {
            targets: 2,
            title: 'Event Type',
            searchable: false,
            orderable: true,
            orderData: [ 2 ],
            render: function(data, type, full, meta) {
                let event = convertToEvent(full);
                return event.event_type;
            }
        },
        {
            targets: 3,
            title: 'Event Time',
            searchable: false,
            orderable: true,
            orderData: [ 3 ],
            render: function(data, type, full, meta) {
                let event = convertToEvent(full);
                return event.event_time;
            }
        },
        {
            targets: 4,
            title: 'Utilizer Type',
            searchable: false,
            orderable: false,
            visible: false,
            render: function(data, type, full, meta) {
                let event = convertToEvent(full);
                return event.utilizer_type;
            }
        },
        {
            targets: 5,
            title: 'Utilizer Id',
            searchable: false,
            orderable: false,
            visible: false,
            render: function(data, type, full, meta) {
                let event = convertToEvent(full);
                return event.utilizer_id;
            }
        },
        {
            targets: -1,
            title: '',
            searchable: false,
            orderable: false,
            render: function(data, type, full, meta) {
                let event = convertToEvent(full);
                return `` +
                    `<div class="text-end">
                        <button id="actionsDropdown_` + event.id + `" type="button" class="btn btn-light btn-active-light-primary btn-sm" style="border: 1px solid #e8e8e8;" data-bs-toggle="dropdown" aria-expanded="false">
                            <i class="fas fa-ellipsis-v"></i>
                        </button>
                        
                        <ul class="dropdown-menu menu menu-sub menu-sub-dropdown menu-column menu-rounded menu-gray-600 menu-state-bg-light-primary fw-bold fs-7 py-3 w-150px" aria-labelledby="actionsDropdown_` + event.id + `">
                            <li>
                                <div class="menu-item px-2">
                                    <a href="/events/` + event.id + `" class="btn btn-active-light-primary btn-sm btn-widest dropdown-button px-4">
                                        <i class="bi bi-pencil-fill me-3"></i>
                                        Details
                                    </a>
                                </div>
                            </li>
                        </ul>                    
                    </div>`;
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
        order: [[3, 'desc']],
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
            let utilizerId = $('input[id="Id"]').val();
            initTable('events_data_table', '/api/events?utilizer_id=' + utilizerId);
        }
    }
}();

KTUtil.onDOMContentLoaded((function() {
    page.init();
}));
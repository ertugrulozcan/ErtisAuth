"use strict";

function convertToApplication(full) {
    return {
        id: full[0],
        name: full[1],
        role: full[2],
        createdAt: full[3],
        createdBy: full[4],
        modifiedAt: full[5],
        modifiedBy: full[6],
    };
}

function initTable(tableId, apiEndpoint) {
    let columnDefinitions = [
        {
            targets: 0,
            searchable: false,
            orderable: false,
            render: function(data, type, full, meta) {
                let application = convertToApplication(full);
                return `` +
                    `<div class="form-check form-check-sm form-check-custom form-check-solid">
                        <input class="form-check-input multi-select-checkbox" type="checkbox" value="1" data-resource-id="` + application.id + `" data-resource-name="` + application.name + `" />
                    </div>`;
            }
        },
        {
            targets: 1,
            title: 'Id',
            searchable: false,
            orderable: true,
            visible: false,
            render: function(data, type, full, meta) {
                let application = convertToApplication(full);
                return `` +
                    `<span>` + application.id + `</span>`;
            }
        },
        {
            targets: 2,
            title: 'Name',
            searchable: true,
            orderable: true,
            orderData: [ 1, 2 ],
            render: function(data, type, full, meta) {
                let application = convertToApplication(full);
                return `` +
                    `<a href="/applications/` + application.id + `">` + application.name + `</a>`;
            }
        },
        {
            targets: 3,
            title: 'Role',
            searchable: false,
            orderable: true,
            render: function(data, type, full, meta) {
                let application = convertToApplication(full);
                return `` +
                    `<div class="badge badge-light-primary fw-bolder">` + application.role + `</div>`;
            }
        },
        {
            targets: 4,
            title: 'Created',
            searchable: false,
            orderable: true,
            orderData: [ 6 ],
            render: function(data, type, full, meta) {
                let application = convertToApplication(full);
                return `` +
                    `<div class="d-flex flex-column">
                        <span>` + application.createdAt + `</span>
                        <span>` + application.createdBy + `</span>
                    </div>`;
            }
        },
        {
            targets: 5,
            title: 'Modified',
            searchable: false,
            orderable: true,
            orderData: [ 8 ],
            render: function(data, type, full, meta) {
                let application = convertToApplication(full);
                return `` +
                    `<div class="d-flex flex-column">
                        <span>` + application.modifiedAt + `</span>
                        <span>` + application.modifiedBy + `</span>
                    </div>`;
            }
        },
        {
            targets: -1,
            title: '',
            searchable: false,
            orderable: false,
            render: function(data, type, full, meta) {
                let application = convertToApplication(full);
                let dropdownHtml = getDatatableDropdown('/applications/' + application.id, 'application', application.id, application.name, true);
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
        order: [[4, 'asc']],
        searchDelay: 600,
        columnDefs: columnDefinitions,
        ajax: {
            url: apiEndpoint,
            type: 'GET'
        }
    });

    let applicationsTableSearchBox = document.getElementById('applicationsTableSearchBox');
    applicationsTableSearchBox.addEventListener("input", function(e) {
        let keyword = applicationsTableSearchBox.value;
        if (keyword.trim().length > 2) {
            dataTable.search(keyword).draw();
        }
        else if (keyword.length === 0) {
            dataTable.search('').draw();
        }
    }, false);
}

let page = function() {
    return {
        init: function () {
            initTable('applications_data_table', '/api/applications');
            setMultiSelectCheckBoxes('applications_data_table');
        }
    }
}();

KTUtil.onDOMContentLoaded((function() {
    page.init();
}));
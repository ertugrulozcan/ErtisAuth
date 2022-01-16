"use strict";

function convertToMembership(full) {
    return {
        id: full[0],
        name: full[1],
        createdAt: full[2],
        createdBy: full[3],
        modifiedAt: full[4],
        modifiedBy: full[5],
    };
}

function initTable(tableId, apiEndpoint) {
    let columnDefinitions = [
        {
            targets: 0,
            searchable: false,
            orderable: false,
            render: function(data, type, full, meta) {
                let membership = convertToMembership(full);
                return `` +
                    `<div class="form-check form-check-sm form-check-custom form-check-solid">
                        <input class="form-check-input multi-select-checkbox" type="checkbox" value="1" data-resource-id="` + membership.id + `" data-resource-name="` + membership.name + `" />
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
                let membership = convertToMembership(full);
                return `` +
                    `<span>` + membership.id + `</span>`;
            }
        },
        {
            targets: 2,
            title: 'Name',
            searchable: true,
            orderable: true,
            orderData: [ 1, 2 ],
            render: function(data, type, full, meta) {
                let membership = convertToMembership(full);
                return `` +
                    `<a href="/memberships/` + membership.id + `">` + membership.name + `</a>`;
            }
        },
        {
            targets: 3,
            title: 'Created',
            searchable: false,
            orderable: true,
            orderData: [ 6 ],
            render: function(data, type, full, meta) {
                let membership = convertToMembership(full);
                return `` +
                    `<div class="d-flex flex-column">
                        <span>` + membership.createdAt + `</span>
                        <span>` + membership.createdBy + `</span>
                    </div>`;
            }
        },
        {
            targets: 4,
            title: 'Modified',
            searchable: false,
            orderable: true,
            orderData: [ 8 ],
            render: function(data, type, full, meta) {
                let membership = convertToMembership(full);
                return `` +
                    `<div class="d-flex flex-column">
                        <span>` + membership.modifiedAt + `</span>
                        <span>` + membership.modifiedBy + `</span>
                    </div>`;
            }
        },
        {
            targets: -1,
            title: '',
            searchable: false,
            orderable: false,
            render: function(data, type, full, meta) {
                let membership = convertToMembership(full);
                let dropdownHtml = getDatatableDropdown('/memberships/' + membership.id, 'membership', membership.id, membership.name, true);
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
        order: [[1, 'asc']],
        searchDelay: 600,
        columnDefs: columnDefinitions,
        ajax: {
            url: apiEndpoint,
            type: 'GET'
        }
    });

    let membershipsTableSearchBox = document.getElementById('membershipsTableSearchBox');
    membershipsTableSearchBox.addEventListener("input", function(e) {
        let keyword = membershipsTableSearchBox.value;
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
            initTable('memberships_data_table', '/api/memberships');
            setMultiSelectCheckBoxes('memberships_data_table');
        }
    }
}();

KTUtil.onDOMContentLoaded((function() {
    page.init();
}));
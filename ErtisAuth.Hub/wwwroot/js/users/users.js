"use strict";

function convertToUser(full) {
    return {
        id: full[0],
        firstName: full[1],
        lastName: full[2],
        fullName: full[1] + ' ' + full[2],
        username: full[3],
        emailAddress: full[4],
        role: full[5],
        createdAt: full[6],
        createdBy: full[7],
        modifiedAt: full[8],
        modifiedBy: full[9],
        photoUrl: full[10],
    };
}

function initTable(tableId, apiEndpoint) {
    let columnDefinitions = [
        {
            targets: 0,
            searchable: false,
            orderable: false,
            render: function(data, type, full, meta) {
                let user = convertToUser(full);
                return `` +
                    `<div class="form-check form-check-sm form-check-custom form-check-solid">
                        <input class="form-check-input multi-select-checkbox" type="checkbox" value="1" data-resource-id="` + user.id + `" data-resource-name="` + user.fullName + `" />
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
                let user = convertToUser(full);
                return `` +
                    `<span>` + user.id + `</span>`;
            }
        },
        {
            targets: 2,
            title: 'User',
            searchable: true,
            orderable: true,
            orderData: [ 1, 2 ],
            render: function(data, type, full, meta) {
                let user = convertToUser(full);
                return `` +
                    `<div class="d-flex align-items-center">
                        <div class="symbol symbol-circle symbol-50px overflow-hidden me-3">
                            <a href="/users/` + user.id + `">
                                <div class="symbol-label">
                                    <img src="` + user.photoUrl + `" alt="` + user.fullName + `" class="w-100"/>
                                </div>
                            </a>
                        </div>
    
                        <div class="d-flex flex-column">
                            <a href="/users/` + user.id + `" class="text-gray-800 text-hover-primary mb-1">` + user.fullName + `</a>
                            <span>` + user.emailAddress + `</span>
                        </div>
                    </div>`;
            }
        },
        {
            targets: 3,
            title: 'First Name',
            searchable: false,
            orderable: true,
            visible: false,
            render: function(data, type, full, meta) {
                let user = convertToUser(full);
                return `` +
                    `<span>` + user.firstName + `</span>`;
            }
        },
        {
            targets: 4,
            title: 'Last Name',
            searchable: false,
            orderable: true,
            visible: false,
            render: function(data, type, full, meta) {
                let user = convertToUser(full);
                return `` +
                    `<span>` + user.lastName + `</span>`;
            }
        },
        {
            targets: 5,
            title: 'Username',
            searchable: true,
            orderable: true,
            orderData: [ 3 ],
            render: function(data, type, full, meta) {
                let user = convertToUser(full);
                return `` +
                    `<span>` + user.username + `</span>`;
            }
        },
        {
            targets: 6,
            title: 'Email Address',
            searchable: true,
            orderable: true,
            visible: false,
            render: function(data, type, full, meta) {
                let user = convertToUser(full);
                return `` +
                    `<span>` + user.emailAddress + `</span>`;
            }
        },
        {
            targets: 7,
            title: 'Role',
            searchable: true,
            orderable: true,
            orderData: [ 5 ],
            render: function(data, type, full, meta) {
                let user = convertToUser(full);
                return `` +
                    `<div class="badge badge-light-primary fw-bolder">` + user.role + `</div>`;
            }
        },
        {
            targets: 8,
            title: 'Created',
            searchable: false,
            orderable: true,
            orderData: [ 6 ],
            render: function(data, type, full, meta) {
                let user = convertToUser(full);
                return `` +
                    `<div class="d-flex flex-column">
                        <span>` + user.createdAt + `</span>
                        <span>` + user.createdBy + `</span>
                    </div>`;
            }
        },
        {
            targets: 9,
            title: 'Modified',
            searchable: false,
            orderable: true,
            orderData: [ 8 ],
            render: function(data, type, full, meta) {
                let user = convertToUser(full);
                return `` +
                    `<div class="d-flex flex-column">
                        <span>` + user.modifiedAt + `</span>
                        <span>` + user.modifiedBy + `</span>
                    </div>`;
            }
        },
        {
            targets: -1,
            title: '',
            searchable: false,
            orderable: false,
            render: function(data, type, full, meta) {
                let user = convertToUser(full);
                let dropdownHtml = getDatatableDropdown('/users/' + user.id, 'user', user.id, user.fullName, true);
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
        order: [[8, 'asc']],
        searchDelay: 600,
        columnDefs: columnDefinitions,
        ajax: {
            url: apiEndpoint,
            type: 'GET'
        }
    });

    let usersTableSearchBox = document.getElementById('usersTableSearchBox');
    usersTableSearchBox.addEventListener("input", function(e) {
        let keyword = usersTableSearchBox.value;
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
            initTable('users_data_table', '/api/users');
            setMultiSelectCheckBoxes('users_data_table');
        }
    }
}();

KTUtil.onDOMContentLoaded((function() {
    page.init();
}));
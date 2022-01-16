"use strict";

let rbac_payload = {
    permissions: [],
    forbidden: []
};

const permissionsTable = document.getElementById('permissionsTable')
const forbiddenTable = document.getElementById('forbiddenTable')

let updateRbacPayload = function () {
    let jsonPayloadInput = document.getElementById('json_payload_input');
    jsonPayloadInput.value = JSON.stringify(rbac_payload);
}

function updatePermissionsJson() {
    rbac_payload['permissions'] = $(permissionsTable).tableToJSON({ ignoreColumns: [4], ignoreHiddenRows: false });
    updateRbacPayload();
}

function updateForbiddenJson() {
    rbac_payload['forbidden'] = $(forbiddenTable).tableToJSON({ ignoreColumns: [4], ignoreHiddenRows: false });
    updateRbacPayload();
}

function createCell(cell, text) {
    let txt = document.createTextNode(text);
    cell.appendChild(txt);
    cell.setAttribute('contenteditable', 'true');
    return cell;
}

function createRemoveButtonCell(cell) {
    let button = document.createElement('button');
    button.setAttribute('type', 'button');

    let classList = ['btn', 'btn-icon-xsm', 'btn-light-danger', 'rbac-remove-button'];
    for (let index in classList) {
        button.classList.add(classList[index]);
    }

    button.addEventListener('click', deleteRow, false);

    button.innerHTML = '<i class="fas fa-times fs-6"></i>';

    cell.appendChild(button);
    return cell;
}

function selectCellText(cell) {
    let range = new Range();
    range.setStart(cell, 0);
    range.setEnd(cell, cell.childNodes.length);
    window.getSelection().removeAllRanges();
    window.getSelection().addRange(range);
}

function addPermission() {
    let row = permissionsTable.getElementsByTagName('tbody')[0].insertRow(permissionsTable.rows.length - 1);

    let cells = [];
    for (let i = 0; i < 4; i++) {
        let cell = createCell(row.insertCell(i), "*");
        cells.push(cell);
    }

    row.setAttribute('data-rbac', '*.*.*.*');
    createRemoveButtonCell(row.insertCell(4));

    updatePermissionsJson();

    cells[0].focus();
    selectCellText(cells[0]);
}

function addPermissionWithRbac(subject, resource, action, object) {
    let row = permissionsTable.getElementsByTagName('tbody')[0].insertRow(permissionsTable.rows.length - 1);

    createCell(row.insertCell(0), subject);
    createCell(row.insertCell(1), resource);
    createCell(row.insertCell(2), action);
    createCell(row.insertCell(3), object);

    row.setAttribute('data-rbac', subject + '.' + resource + '.' + action + '.' + object);
    createRemoveButtonCell(row.insertCell(4));

    updatePermissionsJson();
}

function addForbidden() {
    let row = forbiddenTable.getElementsByTagName('tbody')[0].insertRow(forbiddenTable.rows.length - 1), i;

    let cells = [];
    for (i = 0; i < 4; i++) {
        let cell = createCell(row.insertCell(i), "*");
        cells.push(cell);
    }

    row.setAttribute('data-rbac', '*.*.*.*');
    createRemoveButtonCell(row.insertCell(4));

    updateForbiddenJson();

    cells[0].focus();
    selectCellText(cells[0]);
}

function addForbiddenWithRbac(subject, resource, action, object) {
    let row = forbiddenTable.getElementsByTagName('tbody')[0].insertRow(forbiddenTable.rows.length - 1);

    createCell(row.insertCell(0), subject);
    createCell(row.insertCell(1), resource);
    createCell(row.insertCell(2), action);
    createCell(row.insertCell(3), object);

    row.setAttribute('data-rbac', subject + '.' + resource + '.' + action + '.' + object);
    createRemoveButtonCell(row.insertCell(4));

    updatePermissionsJson();
}

let deleteRow = function() {
    let tableId = $(this).closest('table').attr("id");
    if (tableId === 'permissionsTable') {
        let row = $(this).closest('tr');
        let rbac = row.attr('data-rbac');
        row.remove();
        updatePermissionsJson();
        setBasicRbacSwitchStateToUnchecked(rbac);
        //clearBasicRolesTable(rbac);
    }
    else if (tableId === 'forbiddenTable') {
        let row = $(this).closest('tr');
        let rbac = row.attr('data-rbac');
        row.remove();
        updateForbiddenJson();
    }
};

let updateRowDataRbacAttribute = function(row) {
    let rbacSegments = [];
    row.find('[contenteditable="true"]').each(function() {
        rbacSegments.push($(this).html());
    });

    let newRbac = rbacSegments.join('.');
    row.attr('data-rbac', newRbac);
};

let onRbacSwitchChanged = function(rbac, oldValue, newValue) {
    if (newValue === false) {
        let resourceActionPair;
        if (rbac === '*.roles.read.*') {
            resourceActionPair = 'Roles read';
        }
        else if (rbac === '*.tokens.read.*') {
            resourceActionPair = 'Tokens read';
        }
        else if (rbac === '*.tokens.create.*') {
            resourceActionPair = 'Tokens create';
        }

        if (resourceActionPair) {
            Swal.fire({
                title: 'Warning',
                html: '<strong>' + resourceActionPair + ' permission is required for many operations. It is recommended that you do not remove this permission. This may cause some access problems on some pages or actions.' + '</strong>',
                icon: 'warning',
                showCancelButton: false,
                confirmButtonColor: '#3085d6',
                confirmButtonText: 'Ok'
            });
        }
    }
};

let setBasicRbacSwitchStateToChecked = function(rbac) {
    let rbacSwitch = $('#basicRoleManagementTable').find('input[data-rbac="' + rbac + '"]');
    if (rbacSwitch) {
        rbacSwitch.prop('checked', true);
        onRbacSwitchChanged(rbac, false, true);
    }
};

let setBasicRbacSwitchStateToUnchecked = function(rbac) {
    let rbacSwitch = $('#basicRoleManagementTable').find('input[data-rbac="' + rbac + '"]');
    if (rbacSwitch) {
        rbacSwitch.prop('checked', false);
        onRbacSwitchChanged(rbac, true, false);
    }
};

let clearBasicRolesTable = function(rbac) {
    let resource = rbac.split('.')[1];
    if (!anyRbacByResource(resource)) {
        $('#basicRoleManagementTable').find('tr[data-rbac-resource="' + resource + '"]').remove();
    }
}

let anyRbacByResource = function(resource) {
    let permissionsArray = rbac_payload['permissions'];
    for (let i = 0; i < permissionsArray.length; i++) {
        if (resource === permissionsArray[i]['Resource']) {
            return true;
        }
    }

    return false;
}

jQuery(document).ready(function() {
    let removeButtons = document.getElementsByClassName("rbac-remove-button");
    for (let i = 0; i < removeButtons.length; i++) {
        removeButtons[i].addEventListener('click', deleteRow, false);
    }

    updatePermissionsJson();
    updateForbiddenJson();

    permissionsTable.addEventListener("input", function(e) {
        updatePermissionsJson();

        let row = $(e.target).closest('tr');
        updateRowDataRbacAttribute(row);
        let rbac = row.attr('data-rbac');
        setBasicRbacSwitchStateToChecked(rbac);
    }, false);

    forbiddenTable.addEventListener("input", function(e) {
        updateForbiddenJson();

        let row = $(e.target).closest('tr');
        updateRowDataRbacAttribute(row);
        let rbac = row.attr('data-rbac');
        setBasicRbacSwitchStateToUnchecked(rbac);
    }, false);

    $('.role-switch').change(function() {
        let rbac = $(this).attr('data-rbac');
        let segments = rbac.split('.');
        if (segments && segments.length && segments.length === 4) {
            let subject = segments[0];
            let resource = segments[1];
            let action = segments[2];
            let object = segments[3];

            if (this.checked) {
                addPermissionWithRbac(subject, resource, action, object);
            }
            else {
                let row = $('#permissionsTable').find('tr[data-rbac="' + rbac + '"]');
                if (row) {
                    row.remove();
                    updatePermissionsJson();
                }
            }

            onRbacSwitchChanged(rbac, !this.checked, this.checked);
        }
    });
});
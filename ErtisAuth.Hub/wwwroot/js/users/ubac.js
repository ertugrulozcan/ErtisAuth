"use strict";

let ubac_payload = {
    permissions: [],
    forbidden: []
};

const permissionsTable = document.getElementById('permissionsTable')
const forbiddenTable = document.getElementById('forbiddenTable')

let updateUbacPayload = function () {
    let jsonPayloadInput = document.getElementById('ubac_json_payload_input');
    jsonPayloadInput.value = JSON.stringify(ubac_payload);
}

function updatePermissionsJson() {
    ubac_payload['permissions'] = $(permissionsTable).tableToJSON({ ignoreColumns: [3], ignoreHiddenRows: false });
    updateUbacPayload();
}

function updateForbiddenJson() {
    ubac_payload['forbidden'] = $(forbiddenTable).tableToJSON({ ignoreColumns: [3], ignoreHiddenRows: false });
    updateUbacPayload();
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

    let classList = ['btn', 'btn-icon-xsm', 'btn-light-danger', 'ubac-remove-button'];
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
    for (let i = 0; i < 3; i++) {
        let cell = createCell(row.insertCell(i), "*");
        cells.push(cell);
    }

    row.setAttribute('data-ubac', '*.*.*');
    createRemoveButtonCell(row.insertCell(3));

    updatePermissionsJson();

    cells[0].focus();
    selectCellText(cells[0]);
}

function addPermissionWithUbac(resource, action, object) {
    let row = permissionsTable.getElementsByTagName('tbody')[0].insertRow(permissionsTable.rows.length - 1);
    
    createCell(row.insertCell(0), resource);
    createCell(row.insertCell(1), action);
    createCell(row.insertCell(2), object);

    row.setAttribute('data-ubac', resource + '.' + action + '.' + object);
    createRemoveButtonCell(row.insertCell(3));

    updatePermissionsJson();
}

function addForbidden() {
    let row = forbiddenTable.getElementsByTagName('tbody')[0].insertRow(forbiddenTable.rows.length - 1), i;

    let cells = [];
    for (i = 0; i < 3; i++) {
        let cell = createCell(row.insertCell(i), "*");
        cells.push(cell);
    }

    row.setAttribute('data-ubac', '*.*.*');
    createRemoveButtonCell(row.insertCell(3));

    updateForbiddenJson();

    cells[0].focus();
    selectCellText(cells[0]);
}

function addForbiddenWithUbac(resource, action, object) {
    let row = forbiddenTable.getElementsByTagName('tbody')[0].insertRow(forbiddenTable.rows.length - 1);

    createCell(row.insertCell(0), resource);
    createCell(row.insertCell(1), action);
    createCell(row.insertCell(2), object);

    row.setAttribute('data-ubac', resource + '.' + action + '.' + object);
    createRemoveButtonCell(row.insertCell(3));

    updatePermissionsJson();
}

let deleteRow = function() {
    let tableId = $(this).closest('table').attr("id");
    if (tableId === 'permissionsTable') {
        let row = $(this).closest('tr');
        let ubac = row.attr('data-ubac');
        row.remove();
        updatePermissionsJson();
        setBasicUbacSwitchStateToUnchecked(ubac);
    }
    else if (tableId === 'forbiddenTable') {
        let row = $(this).closest('tr');
        let ubac = row.attr('data-ubac');
        row.remove();
        updateForbiddenJson();
    }
};

let updateRowDataUbacAttribute = function(row) {
    let ubacSegments = [];
    row.find('[contenteditable="true"]').each(function() {
        ubacSegments.push($(this).html());
    });

    let newUbac = ubacSegments.join('.');
    row.attr('data-ubac', newUbac);
};

let onUbacSwitchChanged = function(ubac, oldValue, newValue) {
    if (newValue === false) {
        let resourceActionPair;
        if (ubac === 'roles.read.*') {
            resourceActionPair = 'Roles read';
        }
        else if (ubac === '*.memberships.read.*') {
            resourceActionPair = 'Membership read';
        }
        else if (ubac === 'tokens.read.*') {
            resourceActionPair = 'Tokens read';
        }
        else if (ubac === 'tokens.create.*') {
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

let setBasicUbacSwitchStateToChecked = function(ubac) {
    let ubacSwitch = $('#basicUserPermissionsTable').find('input[data-ubac="' + ubac + '"]');
    if (ubacSwitch) {
        ubacSwitch.prop('checked', true);
        onUbacSwitchChanged(ubac, false, true);
    }
};

let setBasicUbacSwitchStateToUnchecked = function(ubac) {
    let ubacSwitch = $('#basicUserPermissionsTable').find('input[data-ubac="' + ubac + '"]');
    if (ubacSwitch) {
        ubacSwitch.prop('checked', false);
        onUbacSwitchChanged(ubac, true, false);
    }
};

let anyUbacByResource = function(resource) {
    let permissionsArray = ubac_payload['permissions'];
    for (let i = 0; i < permissionsArray.length; i++) {
        if (resource === permissionsArray[i]['Resource']) {
            return true;
        }
    }

    return false;
}

function resetToRoleSettings() {
    let resetToRoleButton = $('#resetToRoleButton');
    let roleId = resetToRoleButton.attr('data-role-id');
    
    // Activate indicator
    resetToRoleButton.attr("data-kt-indicator", "on");
    
    $.get("/api/roles/" + roleId, function(data, status) {
        let originalRolePermissions = data.data.permissions;
        if (originalRolePermissions) {
            $(permissionsTable).find('tbody').empty();
            
            const basicUserPermissionsTable = $('#basicUserPermissionsTable');
            basicUserPermissionsTable.find('input').each(function() {
                if ($(this).hasClass('ubac-switch')) {
                    $(this).prop('checked', false);
                    $(this).removeAttr('edited-for-user');
                }
            });

            basicUserPermissionsTable.find('.edited-for-user-warn-symbol').each(function() {
                $(this).remove();
            });

            for (let i = 0; i < originalRolePermissions.length; i++) {
                let rbac = originalRolePermissions[i];
                let segments = rbac.split('.');
                let subject = segments[0];
                let resource = segments[1];
                let action = segments[2];
                let object = segments[3];

                addPermissionWithUbac(resource, action, object);

                const ubac = resource + '.' + action + '.' + object;
                setBasicUbacSwitchStateToChecked(ubac);
            }
        }

        let originalRoleForbiddens = data.data.forbidden;
        if (originalRolePermissions) {
            $(forbiddenTable).find('tbody').empty();

            for (let i = 0; i < originalRoleForbiddens.length; i++) {
                let rbac = originalRoleForbiddens[i];
                let segments = rbac.split('.');
                let subject = segments[0];
                let resource = segments[1];
                let action = segments[2];
                let object = segments[3];

                addForbiddenWithUbac(resource, action, object);
                const ubac = resource + '.' + action + '.' + object;
                setBasicUbacSwitchStateToUnchecked(ubac);
            }
        }

        updatePermissionsJson();
        updateForbiddenJson();

        resetToRoleButton.removeAttr("data-kt-indicator");
    });
}

jQuery(document).ready(function() {
    let removeButtons = document.getElementsByClassName("ubac-remove-button");
    for (let i = 0; i < removeButtons.length; i++) {
        removeButtons[i].addEventListener('click', deleteRow, false);
    }

    updatePermissionsJson();
    updateForbiddenJson();

    permissionsTable.addEventListener("input", function(e) {
        updatePermissionsJson();

        let row = $(e.target).closest('tr');
        updateRowDataUbacAttribute(row);
        let ubac = row.attr('data-ubac');
        setBasicUbacSwitchStateToChecked(ubac);
    }, false);

    forbiddenTable.addEventListener("input", function(e) {
        updateForbiddenJson();

        let row = $(e.target).closest('tr');
        updateRowDataUbacAttribute(row);
        let ubac = row.attr('data-ubac');
        setBasicUbacSwitchStateToUnchecked(ubac);
    }, false);

    $('.ubac-switch').change(function() {
        let ubac = $(this).attr('data-ubac');
        let segments = ubac.split('.');
        if (segments && segments.length && segments.length === 3) {
            let resource = segments[0];
            let action = segments[1];
            let object = segments[2];

            if (this.checked) {
                addPermissionWithUbac(resource, action, object);
            }
            else {
                let row = $('#permissionsTable').find('tr[data-ubac="' + ubac + '"]');
                if (row) {
                    row.remove();
                    updatePermissionsJson();
                }
            }

            onUbacSwitchChanged(ubac, !this.checked, this.checked);
        }
    });
});
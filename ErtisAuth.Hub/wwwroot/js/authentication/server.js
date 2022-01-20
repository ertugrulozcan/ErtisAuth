"use strict";

const API_VERSION_SEGMENT = "/api/v1";

let servers = [];
let connectedServer = null;

function getLocalStorageKey() {
    let hostname = location.hostname;
    return "ertisauth_hub_servers_" + hostname.replace('.', '_');
}

function updateLocalStorage() {
    if (typeof (Storage) !== "undefined") {
        let key = getLocalStorageKey();
        localStorage.setItem(key, JSON.stringify(servers));
    } else {
        alert('Your browser is not support web storages');
    }
}

function getLocalStorage() {
    if (typeof (Storage) !== "undefined") {
        let key = getLocalStorageKey();
        let json = localStorage.getItem(key);
        return JSON.parse(json);
    } else {
        return null;
    }
}

function addServer(server) {
    let currentServer = getServerByMembershipId(server.membership_id);
    if (currentServer === null) {
        servers.push(server);
        $('#serversDropdown').append(new Option(server.name, server.membership_id));
    }
}

function removeServer(membership_id) {
    if (membership_id && membership_id !== '') {
        let index = findIndexByMembershipId(membership_id);
        if (index > -1) {
            servers.splice(index, 1);

            $('#manageServersTable>.flex-table-item[data-membership-id="' + membership_id + '"]').each(function () {
                $(this).remove();
            });

            $('#serversDropdown>option[value="' + membership_id + '"]').each(function () {
                $(this).remove().end();
            });

            updateLocalStorage();
            forceSelectServer(null);
        }
    }
}

function getServerByMembershipId(membership_id) {
    for (let i = 0; i < servers.length; i++) {
        if (servers[i].membership_id === membership_id) {
            return servers[i];
        }
    }
    
    return null;
}

function findIndexByMembershipId(membership_id) {
    for (let i = 0; i < servers.length; i++) {
        if (servers[i].membership_id === membership_id) {
            return i;
        }
    }

    return -1;
}

function getServerByName(name) {
    for (let i = 0; i < servers.length; i++) {
        if (servers[i].name === name) {
            return servers[i];
        }
    }

    return null;
}

function selectServer(selectedServer) {
    connectedServer = selectedServer;
    updateLoginPanel();

    if (selectedServer) {
        $('#ServerUrlHiddenInput').val(selectedServer.url + API_VERSION_SEGMENT);
        $('#MembershipIdHiddenInput').val(selectedServer.membership_id);
    }
    else {
        $('#ServerUrlHiddenInput').val('');
        $('#MembershipIdHiddenInput').val('');
    }
}

function forceSelectServer(selectedServer) {
    if (selectedServer) {
        $('#serversDropdown>option[value="' + selectedServer.membership_id + '"]').prop('selected', true);
    }
    else {
        $('#serversDropdown>option').each(function() {
            $(this).prop('selected', false);
        });

        $('#serversDropdown>option').first().prop('selected', true);
    }

    selectServer(selectedServer);
}

function updateLoginPanel() {
    let signInPanel = $('#signInPanel');

    if (connectedServer) {
        signInPanel.show();
    }
    else {
        signInPanel.hide();
    }
}

function switchToLoginPanel() {
    let loginPanel = $('#loginPanel');
    let connectNewServerPanel = $('#connectNewServerPanel');
    loginPanel.show();
    connectNewServerPanel.hide();
}

function clearMembershipsDropdown() {
    $('#membershipsDropdown')
        .find('option')
        .remove()
        .end();
}

function switchToConnectPanel() {
    let loginPanel = $('#loginPanel');
    let connectNewServerPanel = $('#connectNewServerPanel');
    loginPanel.hide();
    connectNewServerPanel.show();
    $('#serverUrlInput').val('');
    $('#serverConnectionErrorSpan').text('');
    
    clearMembershipsDropdown();

    $('#membershipSelectionContainer').hide();
    $('#connectNewServer_SaveButton').prop('disabled', true);
}

function switchToManageServersPanel() {
    $('#manageServersPanel').show();
    $('#mainPanel').hide();
    $('#forgotPasswordPanel').hide();

    $('#manageServersTable>.flex-table-item').each(function() {
        $(this).remove();
    });

    for (let i = 0; i < servers.length; i++) {
        let server = servers[i];

        let html = `` +
            `<div class="flex-table-item" data-membership-id="` + server.membership_id + `">
                <div class="flex-table-cell is-bold">
                    <span class="dark-text" style="font-size: 1.1rem;">` + server.name + `</span>
                </div>
                
                <div class="flex-table-cell">
                    <span>` + server.membership_id + `</span>
                </div>

                <div class="flex-table-cell cell-end">
                    <div class="dropdown is-spaced is-dots is-right dropdown-trigger is-pushed-mobile is-down">
                        <div class="is-trigger" aria-haspopup="true">
                            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="feather feather-more-vertical"><circle cx="12" cy="12" r="1"></circle><circle cx="12" cy="5" r="1"></circle><circle cx="12" cy="19" r="1"></circle></svg>
                        </div>
                        <div class="dropdown-menu" role="menu">
                            <div class="dropdown-content">
                                <a href="#" class="dropdown-item is-media remove-server" data-membership-id="` + server.membership_id + `">
                                    <div class="icon">
                                        <i class="lnil lnil-trash-can-alt"></i>
                                    </div>
                                    <div class="meta">
                                        <span>Remove</span>
                                        <span>Remove from list</span>
                                    </div>
                                </a>
                            </div>
                        </div>
                    </div>
                </div>
            </div>`;

        $('#manageServersTable').append(html);
    }

    initDropdowns();

    $('.dropdown-item.remove-server').click(function() {
        let membership_id = $(this).attr('data-membership-id');
        removeServer(membership_id);
    })
}

function switchToMainPanel() {
    $('#mainPanel').show();
    $('#manageServersPanel').hide();
    $('#forgotPasswordPanel').hide();

    updateLoginPanel();
}

function initLocalStorage() {
    let storageData = getLocalStorage();
    if (storageData) {
        servers = storageData;
    }
    else {
        servers = [];
    }
}

function initServersDropdown() {
    for (let i = 0; i < servers.length; i++) {
        let server = servers[i];
        $('#serversDropdown').append(new Option(server.name, server.membership_id));
    }

    $('#serversDropdown').on('change', function () {
        let selectedServerMembershipId = this.value;
        let selectedServer = getServerByMembershipId(selectedServerMembershipId);
        selectServer(selectedServer)
    });
}

function initManageServerPanel() {
    $('#manageServersButton').click(function () {
        switchToManageServersPanel();
    });

    $('#manageServers_ReturnButton').click(function () {
        switchToMainPanel();
    });

    switchToMainPanel();
}

function initServerConnectionPanel() {
    $('#connectServerButton').click(function () {
        switchToConnectPanel();
    });

    $('#connectNewServer_CancelButton').click(function () {
        let loginPanel = $('#loginPanel');
        let connectNewServerPanel = $('#connectNewServerPanel');
        loginPanel.show();
        connectNewServerPanel.hide();
    });

    let membershipSelectionContainer = $('#membershipSelectionContainer');
    let serverConnectionErrorSpan = $('#serverConnectionErrorSpan');
    let membershipsDropdown = $('#membershipsDropdown');
    let connectNewServer_SaveButton = $('#connectNewServer_SaveButton');

    membershipSelectionContainer.hide();
    connectNewServer_SaveButton.prop('disabled', true);

    $('#connectButton').click(function () {
        serverConnectionErrorSpan.text('');
        let serverUrl = $('#serverUrlInput').val();
        if (serverUrl && serverUrl !== '') {
            serverUrl = serverUrl.trim();
            if (serverUrl.endsWith('/')) {
                serverUrl = serverUrl.substring(0, serverUrl.length - 1);
            }

            // Healthcheck
            let healthCheckEndpointUrl = serverUrl + API_VERSION_SEGMENT + '/healthcheck';
            $.ajax({
                url: healthCheckEndpointUrl,
                method: "GET"
            }).done(function (result) {
                // Get memberships
                let getMembershipsEndpointUrl = serverUrl + API_VERSION_SEGMENT + '/server/memberships';
                $.ajax({
                    url: getMembershipsEndpointUrl,
                    method: "GET"
                }).done(function (result) {
                    let selectedMembership = result.items[0];
                    membershipsDropdown.on('change', function () {
                        let selectedMembershipId = this.value;
                        for (let i = 0; i < result.items.length; i++) {
                            let membership = result.items[i];
                            if (membership._id === selectedMembershipId) {
                                selectedMembership = membership;
                                break;
                            }
                        }
                    });

                    membershipSelectionContainer.show();
                    clearMembershipsDropdown();
                    connectNewServer_SaveButton.prop('disabled', false);

                    for (let i = 0; i < result.items.length; i++) {
                        let membership = result.items[i];
                        membershipsDropdown.append(new Option(membership.name, membership._id));
                    }

                    connectNewServer_SaveButton.click(function () {
                        if (selectedMembership) {
                            let server = {
                                url: serverUrl,
                                name: selectedMembership.name,
                                membership_id: selectedMembership._id,
                                secretKey: selectedMembership.secret_key
                            };

                            addServer(server);
                            switchToLoginPanel();
                            forceSelectServer(server);
                            updateLocalStorage();
                        }
                    });
                }).fail(function (jqXHR, textStatus, errorThrown) {
                    serverConnectionErrorSpan.text('Server connection established but membership data could not fetched.');
                });
            }).fail(function (jqXHR, textStatus, errorThrown) {
                serverConnectionErrorSpan.text('Server connection could not be established. Please check the host.');
            });
        }
    });
}

function initLoginPanel() {
    switchToLoginPanel();
    updateLoginPanel();
}

function init() {
    initLocalStorage();
    initServersDropdown();
    initManageServerPanel();
    initServerConnectionPanel();
    initLoginPanel();
}

jQuery(document).ready(function() {
    init();
});
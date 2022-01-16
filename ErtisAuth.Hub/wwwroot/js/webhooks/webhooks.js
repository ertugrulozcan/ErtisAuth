"use strict";

function convertToWebhook(full) {
    return {
        id: full[0],
        name: full[1],
        event: full[2],
        status: full[3],
        createdAt: full[4],
        createdBy: full[5],
        modifiedAt: full[6],
        modifiedBy: full[7],
    };
}

function initTable(tableId, apiEndpoint) {
    let columnDefinitions = [
        {
            targets: 0,
            searchable: false,
            orderable: false,
            render: function(data, type, full, meta) {
                let webhook = convertToWebhook(full);
                return `` +
                    `<div class="form-check form-check-sm form-check-custom form-check-solid">
                        <input class="form-check-input multi-select-checkbox" type="checkbox" value="1" data-resource-id="` + webhook.id + `" data-resource-name="` + webhook.name + `" />
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
                let webhook = convertToWebhook(full);
                return `` +
                    `<span>` + webhook.id + `</span>`;
            }
        },
        {
            targets: 2,
            title: 'Name',
            searchable: true,
            orderable: true,
            orderData: [ 1, 2 ],
            render: function(data, type, full, meta) {
                let webhook = convertToWebhook(full);
                return `` +
                    `<a href="/webhooks/` + webhook.id + `">` + webhook.name + `</a>`;
            }
        },
        {
            targets: 3,
            title: 'Event',
            searchable: false,
            orderable: true,
            render: function(data, type, full, meta) {
                let webhook = convertToWebhook(full);
                return `` +
                    `<div class="badge badge-light-primary fw-bolder">` + webhook.event + `</div>`;
            }
        },
        {
            targets: 4,
            title: 'Status',
            searchable: false,
            orderable: true,
            render: function(data, type, full, meta) {
                let webhook = convertToWebhook(full);
                return `` +
                    `<div class="badge badge-light-primary fw-bolder">` + webhook.status + `</div>`;
            }
        },
        {
            targets: 5,
            title: 'Created',
            searchable: false,
            orderable: true,
            orderData: [ 6 ],
            render: function(data, type, full, meta) {
                let webhook = convertToWebhook(full);
                return `` +
                    `<div class="d-flex flex-column">
                        <span>` + webhook.createdAt + `</span>
                        <span>` + webhook.createdBy + `</span>
                    </div>`;
            }
        },
        {
            targets: 6,
            title: 'Modified',
            searchable: false,
            orderable: true,
            orderData: [ 8 ],
            render: function(data, type, full, meta) {
                let webhook = convertToWebhook(full);
                return `` +
                    `<div class="d-flex flex-column">
                        <span>` + webhook.modifiedAt + `</span>
                        <span>` + webhook.modifiedBy + `</span>
                    </div>`;
            }
        },
        {
            targets: -1,
            title: '',
            searchable: false,
            orderable: false,
            render: function(data, type, full, meta) {
                let webhook = convertToWebhook(full);
                let dropdownHtml = getDatatableDropdown('/webhooks/' + webhook.id, 'webhook', webhook.id, webhook.name, true);
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

    let webhooksTableSearchBox = document.getElementById('webhooksTableSearchBox');
    webhooksTableSearchBox.addEventListener("input", function(e) {
        let keyword = webhooksTableSearchBox.value;
        if (keyword.trim().length > 2) {
            dataTable.search(keyword).draw();
        }
        else if (keyword.length === 0) {
            dataTable.search('').draw();
        }
    }, false);
}

function initCreateWebhookStepper() {
    let createWebhookForm = $("#CreateWebhookForm");
    let nextButton = $('button.stepper-next');
    let previousButton = $('button.stepper-previous');
    let submitButton = $('button[type="submit"].stepper-finish');

    function validateForm(currentStepperContainer) {
        let validator = createWebhookForm.validate();
        let validatedElements = $(currentStepperContainer).find('[data-val-required]');
        let isValid = true;
        validatedElements.each(function() {
            isValid &= validator.element($(this));
        });

        return isValid;
    }

    function onStepForwardChanging() {
        let currentStepperContainer = $('div[data-kt-stepper-element="content"].current');
        return validateForm(currentStepperContainer);
    }

    function onStepBackwardChanging() {

    }

    function onStepChanged() {
        let stepperItem = $('div[data-kt-stepper-element="nav"].stepper-item.current');
        if (stepperItem.is('.stepper-item:first-child')) {
            previousButton.hide();
        }
        else {
            previousButton.show();
        }

        if (stepperItem.is('.stepper-item:last-child')) {
            nextButton.hide();
            submitButton.show();
            updateSummaryPanel();
        }
        else {
            nextButton.show();
            submitButton.hide();
        }

        if (stepperItem.is('.stepper-item:nth-child(2)')) {
            initWebhookRequestBody('webhookRequestBodyEditor');
        }
    }

    function nextStep() {
        let isValid = onStepForwardChanging();
        if (!isValid) {
            return;
        }

        let stepperItem = $('div[data-kt-stepper-element="nav"].stepper-item.current');
        stepperItem.removeClass('current');
        let nextStepperItem = stepperItem.next('div[data-kt-stepper-element="nav"].stepper-item')
        nextStepperItem.addClass('current');

        let stepperContentDiv = $('div[data-kt-stepper-element="content"].current');
        stepperContentDiv.removeClass('current');
        let nextStepperContentDiv = stepperContentDiv.next('div[data-kt-stepper-element="content"]')
        nextStepperContentDiv.addClass('current');
    }

    function previousStep() {
        onStepBackwardChanging();

        let stepperItem = $('div[data-kt-stepper-element="nav"].stepper-item.current');
        stepperItem.removeClass('current');
        let prevStepperItem = stepperItem.prev('div[data-kt-stepper-element="nav"].stepper-item')
        prevStepperItem.addClass('current');

        let stepperContentDiv = $('div[data-kt-stepper-element="content"].current');
        stepperContentDiv.removeClass('current');
        let prevStepperContentDiv = stepperContentDiv.prev('div[data-kt-stepper-element="content"]')
        prevStepperContentDiv.addClass('current');
    }

    function updateSummaryPanel() {
        $('#createWebhookSummary_Name').text($('#createWebhookForm_NameInput').val());
        $('#createWebhookSummary_Description').text($('#createWebhookForm_DescriptionInput').val());
        $('#createWebhookSummary_EventType').text($('#eventTypesDropdown').find(":selected").text());
        $('#createWebhookSummary_TryCount').text($('#tryCountDropdown').find(":selected").text());
        $('#createWebhookSummary_IsActive').text($('#isActiveCheckBox').is(":checked"));
        $('#createWebhookSummary_RequestMethod').text($('#createWebhookForm_RequestMethodDropdown').find(":selected").text());
        $('#createWebhookSummary_RequestUrl').text($('#createWebhookForm_RequestUrlInput').val());

        let headers = [];
        let headersTable = $('#headersTable').tableToJSON({ ignoreColumns: [2], ignoreHiddenRows: false });
        if (headersTable && headersTable.length > 0) {
            for (let i = 0; i < headersTable.length; i++) {
                if (!stringIsNullOrEmpty(headersTable[i]['Key'])) {
                    headers.push({
                        Key: headersTable[i]['Key'],
                        Value: headersTable[i]['Value'],
                    })
                }
            }
        }

        let createWebhookSummary_HeadersTableContainer = $('#createWebhookSummary_HeadersTableContainer');
        createWebhookSummary_HeadersTableContainer.empty();
        if (headers.length > 0) {
            let rowsHtml = '';
            for (let i = 0; i < headers.length; i++) {
                let rowHtml = '<tr><td>' + headers[i].Key + '</td><td>' + headers[i].Value + '</td></tr>';
                rowsHtml += rowHtml;
            }

            let tableHtml = '<table class="mini-table"><thead><tr><th>Key</th><th>Value</th></tr></thead><tbody>' + rowsHtml + '</tbody></table>';
            createWebhookSummary_HeadersTableContainer.html(tableHtml);
        }

        let requestBody = monacoEditorInstances['webhookRequestBodyEditor'].getValue();
        $('#createWebhookSummary_RequestBody').text(requestBody);
    }

    nextButton.on('click', function () {
        nextStep();
        onStepChanged();
    });

    previousButton.on('click', function () {
        previousStep();
        onStepChanged();
    });

    onStepChanged();
}

let page = function() {
    return {
        init: function () {
            initTable('webhooks_data_table', '/api/webhooks');
            setMultiSelectCheckBoxes('webhooks_data_table');
        }
    }
}();

KTUtil.onDOMContentLoaded((function() {
    page.init();

    initCreateWebhookStepper();
}));
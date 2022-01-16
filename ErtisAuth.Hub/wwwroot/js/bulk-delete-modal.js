"use strict";

function bulkDeleteUpdateSubmitButtonState(passwordInput) {
    let deleteModalSubmitButton = $('#bulkDeleteModalSubmitButton');
    if (stringIsNullOrEmpty(passwordInput.val())) {
        deleteModalSubmitButton.addClass('disabled');
        deleteModalSubmitButton.prop("disabled", true);
    }
    else {
        deleteModalSubmitButton.removeClass('disabled');
        deleteModalSubmitButton.prop("disabled", false);
    }
}

KTUtil.onDOMContentLoaded((function() {
    $('#bulkDeleteModal').on('show.bs.modal', function (event)
    {
        let modal = $(this);
        let target = $(event.relatedTarget);

        let passwordInput = modal.find('#bulkDeleteModalPasswordInput');
        passwordInput.val('');
        bulkDeleteUpdateSubmitButtonState(passwordInput);

        let selectedIds = selectedResources.map(function(item) { return item.id; });
        modal.find('#bulkDeleteModalItemIdsJsonInput').val(JSON.stringify(selectedIds));

        let selectedNames = selectedResources.map(function(item) { return item.name; });
        let bulkDeleteModalSelectedItemsNameList = modal.find('#bulkDeleteModalSelectedItemsNameList');
        bulkDeleteModalSelectedItemsNameList.empty();

        for (let i = 0; i < selectedNames.length; i++) {
            bulkDeleteModalSelectedItemsNameList.append($('<li>').append(selectedNames[i]));
        }
    });

    $('#bulkDeleteAllRadioButton').change(function() {
        if (this.checked) {
            $('#bulkDeleteAllInput').val(true);
        }
        else {
            $('#bulkDeleteAllInput').val(false);
        }
    });

    $('#bulkDeleteModalPasswordInput').on('keyup', function(e) {
        bulkDeleteUpdateSubmitButtonState($(this));
    });
}));
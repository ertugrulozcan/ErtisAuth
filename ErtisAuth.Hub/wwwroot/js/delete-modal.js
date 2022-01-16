"use strict";

function getDeleteModalButtonAttributes(id, typeName, resourceName, contentId, languageCode, languageName) {
    let modalTitle = "Delete " + typeName;
    let bodyTitle = "Delete: " + "<strong>" + resourceName + "</strong>";
    let bodyText = "This action cannot be undone. This will permanently delete the " + typeName + " and remove all linked resources.";
    let submitButtonText = "I understand the consequences, delete this " + typeName;
    
    if (contentId && languageCode && languageName) {
        return `` +
            `data-bs-toggle="modal" 
            data-bs-target="#itemDeleteModal"
            data-pass-id="` + id + `"
            data-pass-content-id="` + contentId + `"
            data-pass-language-code="` + languageCode + `"
            data-pass-language-name="` + languageName + `"
            data-pass-modal-title="` + modalTitle + `"
            data-pass-body-title="` + bodyTitle + `"
            data-pass-body-text="` + bodyText + `"
            data-pass-submit-button-text="` + submitButtonText + `"`;
    }
    else {
        return `` +
            `data-bs-toggle="modal" 
            data-bs-target="#itemDeleteModal"
            data-pass-id="` + id + `"
            data-pass-modal-title="` + modalTitle + `"
            data-pass-body-title="` + bodyTitle + `"
            data-pass-body-text="` + bodyText + `"
            data-pass-submit-button-text="` + submitButtonText + `"`;
    }
}

function updateSubmitButtonState(passwordInput) {
    let deleteModalSubmitButton = $('#deleteModalSubmitButton');
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
    $('#itemDeleteModal').on('show.bs.modal', function (event)
    {
        let modal = $(this);
        let target = $(event.relatedTarget);

        let passwordInput = modal.find('#deleteModalPasswordInput');
        passwordInput.val('');
        updateSubmitButtonState(passwordInput);

        modal.find('#deleteModalItemIdInput').val(target.data('pass-id'));
        modal.find('#deleteModalItemContentIdInput').val(target.data('pass-content-id'));
        modal.find('#deleteModalTitle').text(target.data('pass-modal-title'));
        modal.find('#deleteModalBodyTitle').html(target.data('pass-body-title'));
        modal.find('#deleteModalBodyText').html(target.data('pass-body-text'));
        modal.find('#deleteModalSubmitButton').text(target.data('pass-submit-button-text'));
        modal.find('#deleteModalLanguageNameSpan').html(target.data('pass-language-name'));
    });

    $('#deleteAllRadioButton').change(function() {
        if (this.checked) {
            $('#deleteAllInput').val(true);
        }
        else {
            $('#deleteAllInput').val(false);
        }
    });

    $('#deleteModalPasswordInput').on('keyup', function(e) {
        updateSubmitButtonState($(this));
    });
}));
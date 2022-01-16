"use strict";

let selectedResources = [];

function onCheckboxChanged() {
    const deleteSelectedContainer = $('div[multi-select-toolbox="delete_selected_container"]');
    const selectedCountSpan = $('span[multi-select-toolbox="selected_count"]');
    selectedCountSpan.text(selectedResources.length);
    
    if (selectedResources.length > 0) {
        deleteSelectedContainer.removeClass('d-none');
    }
    else {
        deleteSelectedContainer.addClass('d-none');
    }
}

function findSelectedIndexById(resourceId) {
    for (let i = 0; i < selectedResources.length; i++) {
        if (selectedResources[i].id === resourceId) {
            return i;
        }
    }
    
    return -1;
}

function setMultiSelectCheckBoxes(tableId) {
    let table = $('#' + tableId);
    table.on('draw.dt', function() {
        table.find('.multi-select-checkbox').each(function() {
            let checkbox = $(this);
            let resourceId = checkbox.attr('data-resource-id');
            let resourceName = checkbox.attr('data-resource-name');
            
            checkbox.change(function() {
                if (this.checked) {
                    let index = findSelectedIndexById(resourceId);
                    if (index < 0) {
                        selectedResources.push({
                            id: resourceId,
                            name: resourceName
                        });
                    }
                }
                else {
                    let index = findSelectedIndexById(resourceId);
                    if (index >= 0) {
                        selectedResources.splice(index, 1);   
                    }
                }

                onCheckboxChanged();
            });
        });

        table.find('.select-all-multi-select-checkbox').each(function() {
            let selectAllCheckbox = $(this);
            selectAllCheckbox.change(function() {
                let isChecked = this.checked;
                table.find('.multi-select-checkbox').each(function() {
                    let checkbox = $(this);
                    checkbox.prop('checked', isChecked).change();
                });
            });
        });
    });
}
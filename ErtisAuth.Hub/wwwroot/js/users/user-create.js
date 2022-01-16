const form = document.getElementById('CreateUserForm');

function onSubmit(event) {
    event.preventDefault();

    let validator = FormValidation.formValidation(
        form,
        {
            fields: {
                'Password': {
                    validators: {
                        notEmpty: {
                            message: 'The password is required'
                        },
                        callback: {
                            message: 'Please enter valid password',
                            callback: function (input) {
                                if (input.value.length > 0) {
                                    return validatePassword();
                                }
                            }
                        }
                    }
                },
                'PasswordAgain': {
                    validators: {
                        notEmpty: {
                            message: 'Confirm password is required'
                        },
                        identical: {
                            compare: function () {
                                return form.querySelector('[name="Password"]').value;
                            },
                            message: 'These passwords don\'t match'
                        }
                    }
                },
            },

            plugins: {
                trigger: new FormValidation.plugins.Trigger(),
                bootstrap: new FormValidation.plugins.Bootstrap5({
                    rowSelector: '.fv-row',
                    eleInvalidClass: '',
                    eleValidClass: ''
                })
            }
        }
    );
    
    if (validator) {
        validator.validate().then(function (status) {
            if (status === 'Valid') {
                form.submit();
            }
            else {
                
            }
        });
    }
}

function clearForm() {
    $('#createUserForm_firstNameInput').val('');
    $('#createUserForm_lastNameInput').val('');
    $('#createUserForm_userNameInput').val('');
    $('#createUserForm_emailAddressInput').val('');
    $('#Password').val('');
    $('#PasswordAgain').val('');

    let passwordMeterElement = document.querySelector("#kt_password_meter_control");
    let passwordMeter = KTPasswordMeter.getInstance(passwordMeterElement);
    passwordMeter.reset();
}

jQuery(document).ready(function() {
    $('#createUserModal').on('show.bs.modal', function (event) {
        clearForm();
    });
    
    form.addEventListener('submit', onSubmit);
});
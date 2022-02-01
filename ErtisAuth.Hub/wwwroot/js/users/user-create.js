const form = document.getElementById('CreateUserForm');

function onSubmit(event) {
    event.preventDefault();

    let validator = FormValidation.formValidation(
        form,
        {
            fields: {
                'createUserForm_emailAddressInput': {
                    validators: {
                        emailAddress: {
                            message: 'The value is not a valid email address'
                        }
                    }
                },
                'createUserForm_userNameInput': {
                    validators: {
                        callback: {
                            message: 'The username is required. Username can not starts with a digit and can not contains space',
                            callback: function (input) {
                                return input.value && input.value !== '' && !(/^\d+$/.test(input.value.charAt(0)));
                            },
                        }
                    }
                },
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

function resetForm() {
    $('#createUserForm_firstNameInput').val('');
    $('#createUserForm_lastNameInput').val('');
    $('#createUserForm_userNameInput').val('');
    $('#createUserForm_emailAddressInput').val('');
    $('#rolesDropdown>option:eq(0)').prop('selected', true);
    $('#Password').val('');
    $('#PasswordAgain').val('');

    let passwordMeterElement = document.querySelector("#kt_password_meter_control");
    let passwordMeter = KTPasswordMeter.getInstance(passwordMeterElement);
    passwordMeter.reset();
}


function setAutoFillUsername() {
    let firstName = '';
    let lastName = '';
    
    function setUsername() {
        let username = '';
        if (firstName === '' && lastName === '')
            username = '';
        else if (firstName === '')
            username = slugify(lastName, true);
        else if (lastName === '')
            username = slugify(firstName, true);
        else
            username = slugify(firstName, true) + '.' + slugify(lastName, true);

        $('#createUserForm_userNameInput').val(username);
    }
    
    $('#createUserForm_firstNameInput').on('input', function() {
        firstName = $(this).val();
        setUsername();
    });

    $('#createUserForm_lastNameInput').on('input', function() {
        lastName = $(this).val();
        setUsername();
    });
}

jQuery(document).ready(function() {
    $('#createUserModal').on('show.bs.modal', function (event) {
        resetForm();
    });
    
    setAutoFillUsername();
    
    form.addEventListener('submit', onSubmit);
});
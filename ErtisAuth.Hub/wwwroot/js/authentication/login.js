"use strict";

function initPageLoader() {
    let pageloader = $(".pageloader");
    pageloader.length && (pageloader.toggleClass("is-active"), $(window).on("load", (function() {
        let e = setTimeout((function() {
            pageloader.toggleClass("is-active"), $(".infraloader").toggleClass("is-active"), clearTimeout(e), setTimeout((function() {
                $(".rounded-hero").addClass("is-active");
            }), 350)
        }), 700);
        $(".infraloader").removeClass("is-active");
    })))
}

function setUserInformations() {
    $('#userAgentRawHiddenInput').val(window.navigator.userAgent);

    $('login-submit').prop('disabled', true);
    getIPInfo(
        function(ip) {
            $('#ipAddressHiddenInput').val(ip);
            $('#ipAddressSpan').text(ip);
            
            $('login-submit').prop('disabled', false);
        },
        function(error) {
            console.log('IP address could not fetched! : ' + error);
            $('login-submit').prop('disabled', false);
        }
    );
}

function updateLoginButtonState() {
    let usernameInput = $("#Username");
    let passwordInput = $("#Password");
    let loginButton = $("#login-submit");
    if (usernameInput.val().length === 0 || passwordInput.val().length === 0) {
        loginButton.addClass('is-disabled');
    }
    else {
        loginButton.removeClass('is-disabled');
    }
}

function onSubmitCancelled() {
    let loginButton = $("#login-submit");
    loginButton.removeClass("is-loading");
}

function switchToForgotPasswordPanel() {
    $('#forgotPasswordEmailInput').val('');
    forgotPasswordSetState('ready');

    $('#forgotPasswordPanel').show();
    $('#mainPanel').hide();
    $('#manageServersPanel').hide();
}

function forgotPasswordSetState(state, message) {
    if (state === 'ready') {
        $('#forgotPasswordErrorSpan').text('');
        $('#forgotPasswordErrorSpan').hide();
        $('#forgotPasswordSuccessSpan').text('');
        $('#forgotPasswordSuccessSpan').hide();
        $('#forgotPassword_SubmitButton').removeClass('is-loading');
        $('#forgotPassword_SubmitButton').prop('disabled', false);
        $('#forgotPassword_CancelButton').prop('disabled', false);
    }
    else if (state === 'pending') {
        $('#forgotPasswordErrorSpan').text('');
        $('#forgotPasswordErrorSpan').hide();
        $('#forgotPasswordSuccessSpan').text('');
        $('#forgotPasswordSuccessSpan').hide();
        $('#forgotPassword_SubmitButton').addClass('is-loading');
        $('#forgotPassword_SubmitButton').prop('disabled', true);
        $('#forgotPassword_CancelButton').prop('disabled', true);
    }
    else if (state === 'error') {
        $('#forgotPasswordErrorSpan').text(message);
        $('#forgotPasswordErrorSpan').show();
        $('#forgotPasswordSuccessSpan').text('');
        $('#forgotPasswordSuccessSpan').hide();
        $('#forgotPassword_SubmitButton').removeClass('is-loading');
        $('#forgotPassword_SubmitButton').prop('disabled', false);
        $('#forgotPassword_CancelButton').prop('disabled', false);
    }
    else if (state === 'success') {
        $('#forgotPasswordErrorSpan').text('');
        $('#forgotPasswordErrorSpan').hide();
        $('#forgotPasswordSuccessSpan').text(message);
        $('#forgotPasswordSuccessSpan').show();
        $('#forgotPassword_SubmitButton').removeClass('is-loading');
        $('#forgotPassword_SubmitButton').prop('disabled', false);
        $('#forgotPassword_CancelButton').prop('disabled', false);
    }
}

function resetPasswordSubmit() {
    if (connectedServer) {
        let emailAddress = $('#forgotPasswordEmailInput').val();
        let encryptedSecretKey = connectedServer.secretKey;
        let membershipId = connectedServer.membership_id;
        let host = location.origin;

        let serverUrl = connectedServer.url;
        serverUrl = serverUrl.trim();
        if (serverUrl.endsWith('/')) {
            serverUrl = serverUrl.substring(0, serverUrl.length - 1);
        }

        serverUrl = serverUrl + API_VERSION_SEGMENT

        if (emailAddress && emailAddress !== '' && emailAddress.trim() && emailAddress !== '' && emailAddress.includes('@')) {
            forgotPasswordSetState('pending');

            $.ajax({
                contentType: 'application/json',
                data: JSON.stringify({
                    "emailAddress": emailAddress,
                    "serverUrl": serverUrl,
                    "encryptedSecretKey": encryptedSecretKey,
                    "membershipId": membershipId,
                    "host": host
                }),
                success: function (data) {
                    forgotPasswordSetState('success', 'We have sent you an e-mail with your reset password link. Please check your mailbox and follow the instructions.');
                    Swal.fire({
                        title: 'Reset Password',
                        html: 'We have sent you an e-mail with your reset password link. Please check your mailbox and follow the instructions.',
                        type: 'success',
                        showCancelButton: false,
                        confirmButtonColor: '#3085d6',
                        confirmButtonText: 'Ok'
                    });
                    switchToMainPanel();
                },
                error: function (param1, param2, param3) {
                    forgotPasswordSetState('error', param1.responseText);
                },
                processData: false,
                type: 'POST',
                url: '/api/forgot-password'
            });
        }
        else {
            forgotPasswordSetState('error', 'Please enter a valid email address');
        }
    }
    else {
        forgotPasswordSetState('error', 'Connected server error!');
    }
}

initPageLoader();
jQuery(document).ready(function() {
    $("body").removeClass("is-dark");

    $("#login-submit").on("click", function() {
        let loginButton = $(this);
        loginButton.addClass("is-loading");
    });

    $("#Username").on("change", function() {
        updateLoginButtonState();
    });

    $("#Password").on("change", function() {
        updateLoginButtonState();
    });
    
    setUserInformations();
    setTimeout(updateLoginButtonState, 500);

    $('#forgotPasswordButton').click(function () {
        switchToForgotPasswordPanel();
    });

    $('#forgotPassword_CancelButton').click(function () {
        switchToMainPanel();
    });

    $('#forgotPassword_SubmitButton').click(function () {
        resetPasswordSubmit();
    });

    let serverUrlInput = document.getElementById("serverUrlInput");
    serverUrlInput.addEventListener("keyup", function(event) {
        if (event.keyCode === 13) {
            event.preventDefault();
            document.getElementById("connectButton").click();
        }
    });
});
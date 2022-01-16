"use strict";

function initPageLoader() {
    let pageloader = $(".pageloader");
    pageloader.length && (pageloader.toggleClass("is-active"), $(window).on("load", (function() {
        let e = setTimeout((function() {
            pageloader.toggleClass("is-active"), $(".infraloader").toggleClass("is-active"), clearTimeout(e), setTimeout((function() {
                $(".rounded-hero").addClass("is-active")
            }), 350)
        }), 700)
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
    if (usernameInput.val().length == 0 || passwordInput.val().length == 0) {
        loginButton.addClass('is-disabled');
    }
    else {
        loginButton.removeClass('is-disabled');
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
});
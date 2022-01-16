"use strict";

function generateRandomKey(length) {
    let result = '';
    let characters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    let charactersLength = characters.length;
    for (let i = 0; i < length; i++) {
        result += characters.charAt(Math.floor(Math.random() * charactersLength));
    }

    return result;
}

function generateRandomSecretKey() {
    const secretKey = generateRandomKey(32);
    $('#membershipSecretKeyInput').val(secretKey);
}
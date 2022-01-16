"use strict";

function generateRandomText(length) {
    let result           = [];
    let characters       = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
    let charactersLength = characters.length;
    for (let i = 0; i < length; i++) {
        result.push(characters.charAt(Math.floor(Math.random() * charactersLength)));
    }

    return result.join('');
}

function stringIsNullOrEmpty(str) {
    if (str) {
        str = str + '';
        return str.length === 0 || !str.trim() || /^\s*$/.test(str);
    }

    return true;
}
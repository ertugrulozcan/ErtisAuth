"use strict";

KTUtil.onDOMContentLoaded((function() {
    let mailTemplateHtmlInput = document.getElementById('mail_template_html_input');
    $('#mailTemplatePreviewFrame').contents().find('html').html(mailTemplateHtmlInput.value);
    
    initMonacoEditor('mailHookRequestBodyEditor', 'html', '../../../../', mailTemplateHtmlInput.value, function(code) {
        $('#mailTemplatePreviewFrame').contents().find('html').html(code);
        let mailTemplateHtmlInput = document.getElementById('mail_template_html_input');
        mailTemplateHtmlInput.value = code;
    });
}));
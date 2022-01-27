"use strict";

const monacoEditorInstances = {};

let isInitializedMonacoEditor = false;
function initMonacoEditor(elementId, language, wwwroot, content, onContentChaged) {
    if (isInitializedMonacoEditor) {
        return;
    }

    require.config({ paths: { 'vs': wwwroot + 'lib/monaco-editor/vs' }});
    
    if (!wwwroot) {
        wwwroot = '';
    }

    if (!content) {
        content = '';
    }
    
    let lines = content.split('\n');

    require(["vs/editor/editor.main"], function () {
        let editor = monaco.editor.create(document.getElementById(elementId), {
            value: lines.join('\n'),
            language: language,
            theme: 'vs',
            quickSuggestions: { other: true, comments: true, strings: true },
            minimap: {
                enabled: false
            }
        });

        editor.getModel().onDidChangeContent((event) => {
            if (onContentChaged) {
                onContentChaged(editor.getValue())
            }
        });

        monacoEditorInstances[elementId] = editor;
    });

    isInitializedMonacoEditor = true;
}
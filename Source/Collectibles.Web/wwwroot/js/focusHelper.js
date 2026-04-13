window.focusFirstInput = function (cellId) {
    var cell = document.getElementById(cellId);
    if (cell) {
        var input = cell.querySelector('input,textarea,select');
        if (input) input.focus();
    }
};

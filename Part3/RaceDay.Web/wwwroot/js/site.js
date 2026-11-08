// RaceDay: small bits of page behaviour (the pages work without JavaScript too).
document.addEventListener('DOMContentLoaded', function () {
    // Registration: only show the organiser code box when "Organiser" is chosen
    var roleSelect = document.getElementById('Role');
    var codeBox = document.getElementById('organiser-code-box');
    if (roleSelect && codeBox) {
        var toggleCodeBox = function () {
            codeBox.style.display = roleSelect.value === 'Organiser' ? 'block' : 'none';
        };
        roleSelect.addEventListener('change', toggleCodeBox);
        toggleCodeBox();
    }
});


function datepicker() {
    $('.date-picker').datepicker({
        maxDate: "-1D",
        dateFormat: "d.m.yy",
        changeMonth: true,
        changeYear: true,
        yearRange: "-50:+0"
    });
}

$(document).ready(function () {
    datepicker();
});
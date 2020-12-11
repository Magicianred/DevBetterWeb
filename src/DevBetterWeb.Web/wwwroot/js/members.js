$(function () {

    $('#membersTable').DataTable({ "pageLength" : 25 });

    $('#datepicker').datepicker({ changeYear: false, dateFormat: 'dd/mm' });
});
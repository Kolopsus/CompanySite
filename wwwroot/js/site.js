$(document).ready(function() {
    var currentPath = window.location.pathname;
    $('.menu-section a').each(function() {
        var href = $(this).attr('href');
        if (currentPath === href || (currentPath === '/' && href.endsWith('/Home'))) {
            $(this).addClass('active');
        }
    });
});
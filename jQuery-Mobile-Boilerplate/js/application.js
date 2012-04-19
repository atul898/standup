/* 
	jQuery Mobile Boilerplate
	application.js
*/
$(document).live("pageinit", function(event){
	// custom code goes here



});



//http://dev.jtsage.com/jQM-DateBox/demos/script/maxdays.html
$('#page').live('pagecreate', function (event) {
    // Default picker value of Jan 1, 2012
    var defaultPickerValue = [2012, 0, 1];

    // Make it a date
    var presetDate = new Date(defaultPickerValue[0], defaultPickerValue[1], defaultPickerValue[2], 0, 0, 0, 0);

    // Get Today
    var todaysDate = new Date();

    // Length of 1 Day
    var lengthOfDay = 24 * 60 * 60 * 1000;

    // Get the difference
    var diff = parseInt((((presetDate.getTime() - todaysDate.getTime()) / lengthOfDay) + 1) * -1, 10);

    // Set the origin date
    $('#mydate').data('datebox').options.defaultPickerValue = defaultPickerValue;

    // Set minDays to disallow anything earlier
    $('#mydate').data('datebox').options.minDays = diff;
});
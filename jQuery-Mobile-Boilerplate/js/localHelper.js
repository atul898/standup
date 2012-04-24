
//http://atulc-e6500-pc/Where/YaharaEmployeeStatusService.svc/Json/WelcomeMessage?message=%22Hello%20World!%22


function callGetStandupStatus(selectedDate) {

    //Clear list
    $("#messageList").empty();

    //Add the 'Loading' header
    var row = '<li data-role="list-divider">' + 'Contacting Outlook server....' + '</li>';
    $("#messageList").append(row);

    $("#messageList").listview('refresh');

    var link = "http://10.111.124.47:8000//YaharaEmployeeStatusService/Json/GetStatus?date=" + selectedDate + "&callback=?"
    //var link = "http://localhost:51635/YaharaEmployeeStatusService.svc/Json/GetStatus?date=" + selectedDate + "&callback=?"
    //var link = "http://localhost:8000/YaharaEmployeeStatusService/Json/GetStatus?date=" + selectedDate + "&callback=?"
    //var link = "http://atulc-e6500-pc/Where/YaharaEmployeeStatusService.svc/Json/GetStatus?date=" + selectedDate + "&callback=?"
    //var link = "http://atulc-e6500-pc/Where/YaharaEmployeeStatusService.svc/Json/GetStatus?date=" + selectedDate + "&callback=?"
    //var link = "http://localhost:51635/YaharaEmployeeStatusService.svc/Json/GetStatus?date=" + selectedDate + "&callback=?"
    //var link = "http://192.168.1.107/Where/YaharaEmployeeStatusService.svc/Json/GetStatus?date=" + selectedDate + "&callback=?"


    $.getJSON(link)
    .success(
     function (data) {

         $("ul").fadeTo(0, 0.0);

         //Log
         console.log(data.Date);

         //Clear list
         $("#messageList").empty();

         //Add the header
         var row = '<li data-role="list-divider" id="dateHeader">' + data.DisplayDate
                    + '<span class="ui-li-count" id="itemCount">' + data.ListOfItems.length
                    + '</span></li>'
         $("#messageList").append(row);

         for (var i = 0; i < data.ListOfItems.length; i++) {
             console.log(data.ListOfItems[i].Name);

             //Each item looks like this    
             //            <li><a href="index.html">
             //                <h3>data.ListOfItems[i].Name</h3>
             //                <p><strong>data.ListOfItems[i].Subject</strong></p>
             //                <p>data.ListOfItems[i].Message</p>
             //                <p class="ui-li-aside"><strong>6:24</strong>PM</p>
             //                </a>
             //            </li>

             var row = '<li><a href="#detailPage?item=' + i + '" rel="external"><h3>' + data.ListOfItems[i].Name + '        ' +
                '</h3><p><strong>' + data.ListOfItems[i].Subject +
                '</strong></p><p>' + data.ListOfItems[i].MessageBody +
                '</p><p class="ui-li-aside"><strong>' + data.ListOfItems[i].DisplayTime + '</strong></p></a> </li>';


             sessionStorage.setItem('dataDate', data.Date);
             sessionStorage.setItem('dataDisplayDate', data.DisplayDate);
             sessionStorage.setItem('data' + i + 'DisplayTime', data.ListOfItems[i].DisplayTime);
             sessionStorage.setItem('data' + i + 'Subject', data.ListOfItems[i].Subject);
             sessionStorage.setItem('data' + i + 'MessageBody', data.ListOfItems[i].MessageBody);
             sessionStorage.setItem('data' + i + 'MessageHTMLBody', data.ListOfItems[i].MessageHTMLBody);
             sessionStorage.setItem('data' + i + 'Name', data.ListOfItems[i].Name);

             $("#messageList").append(row);
         }

         $("#messageList").listview('refresh');
         $("ul").fadeTo(100, 0.1);
         $("ul").fadeTo(500, 1.0);

         sessionStorage.setItem('bbb', 'bye');
     })

    .error(
     function () {
         //Clear list
         $("#messageList").empty();

         //Add the header
         var row = '<li data-role="list-divider">' + 'Err! Please try again later.' + '</li>';
         $("#messageList").append(row);

         $("#messageList").listview('refresh');
     })

    .complete(
     function () {
         //alert("complete");
     });
 }

 //reset type=date inputs to text
 $(document).bind("mobileinit", function () {
     $.mobile.page.prototype.options.degradeInputs.date = true;
 });

 $(document).ready(function () {

     localStandupDateVariable = new Date();

     var curr_date = localStandupDateVariable.getDate();
     var curr_month = localStandupDateVariable.getMonth();
     var curr_year = localStandupDateVariable.getFullYear();
     var selectedDate =
                ("0" + (localStandupDateVariable.getMonth() + 1)).slice(-2) + "" +
                ("0" + localStandupDateVariable.getDate()).slice(-2) + "" +
                localStandupDateVariable.getFullYear();

     $("#standupPage").live('pageinit', function (event) {
         if (event.type == "pageinit") {
             callGetStandupStatus(selectedDate);
         }
     });

     $("#frondeskPage").live('pageshow', function (event) {
         if (event.type == "pageshow") {
             $("textarea#textarea").val('');

             var link = "http://10.111.124.47:8000//YaharaEmployeeStatusService/Json/ReadCurrentMessage?" + "callback=?"
             //var link = "http://localhost:51635/YaharaEmployeeStatusService.svc/Json/ReadCurrentMessage?" + "callback=?"

             $.getJSON(link)
                .success(
                 function (data) {
                     var elem1 = document.getElementById("currentMessage");
                     elem1.innerHTML = '<p>' + data.Message + '</p>';
                 })
                .error(
                 function () {
                     var elem1 = document.getElementById("currentMessage");
                     elem1.innerHTML = 'Could not retreive data';
                 })
                .complete(
                 function () {
                 });
         }
     });

     $("#standupPage").live('swipeleft swiperight', function (event) {
         if (event.type == "swiperight") {
             localStandupDateVariable = localStandupDateVariable.addDays(-1);
             var curr_date = localStandupDateVariable.getDate();
             var curr_month = localStandupDateVariable.getMonth();
             var curr_year = localStandupDateVariable.getFullYear();
             var selectedDate =
                        ("0" + (localStandupDateVariable.getMonth() + 1)).slice(-2) + "" +
                        ("0" + localStandupDateVariable.getDate()).slice(-2) + "" +
                        localStandupDateVariable.getFullYear();
             callGetStandupStatus(selectedDate);

             //             var prev = $("#previndex", $.mobile.activePage);
             //             var previndex = $(prev).data("index");
             //             if (previndex != '') {
             //                 $.mobile.changePage({ url: "index.php", type: "get", data: "index=" + previndex }, "slide", true);
             //             }
         }
         if (event.type == "swipeleft") {
             localStandupDateVariable = localStandupDateVariable.addDays(1);
             var curr_date = localStandupDateVariable.getDate();
             var curr_month = localStandupDateVariable.getMonth();
             var curr_year = localStandupDateVariable.getFullYear();
             var selectedDate =
                        ("0" + (localStandupDateVariable.getMonth() + 1)).slice(-2) + "" +
                        ("0" + localStandupDateVariable.getDate()).slice(-2) + "" +
                        localStandupDateVariable.getFullYear();
             callGetStandupStatus(selectedDate);

             //             var next = $("#nextindex", $.mobile.activePage);
             //             var nextindex = $(next).data("index");
             //             if (nextindex != '') {
             //                 $.mobile.changePage({ url: "index.php", type: "get", data: "index=" + nextindex });
             //             }
         }
         event.preventDefault();
     });

     $("#tpPage").live('swipeleft swiperight', function (event) {
         if (typeof localTPDateVariable === "undefined") {
             event.preventDefault();
             return;
         }
         if (event.type == "swiperight") {
             localTPDateVariable = localTPDateVariable.addDays(-1);
             var curr_date = localTPDateVariable.getDate();
             var curr_month = localTPDateVariable.getMonth();
             var curr_year = localTPDateVariable.getFullYear();
             var selectedDate =
                        localTPDateVariable.getFullYear() + "-" +
                        ("0" + (localTPDateVariable.getMonth() + 1)).slice(-2) + "-" +
                        ("0" + localTPDateVariable.getDate()).slice(-2);
             $("input#tpDateBox").val(selectedDate);
             tpDateValueSet($("input#tpDateBox")[0]);
         }
         if (event.type == "swipeleft") {
             localTPDateVariable = localTPDateVariable.addDays(1);
             var curr_date = localTPDateVariable.getDate();
             var curr_month = localTPDateVariable.getMonth();
             var curr_year = localTPDateVariable.getFullYear();
             var selectedDate =
                        localTPDateVariable.getFullYear() + "-" +
                        ("0" + (localTPDateVariable.getMonth() + 1)).slice(-2) + "-" +
                        ("0" + localTPDateVariable.getDate()).slice(-2);
             $("input#tpDateBox").val(selectedDate);
             tpDateValueSet($("input#tpDateBox")[0]);
         }
         event.preventDefault();
     });


     $("#confirmSubmit").click(function (event) {
         $("#sliderYesNo")[0].selectedIndex == 0;
         $("#changeWelcomeMessage").fadeTo(0, 0.5);
         $("#changeWelcomeMessage").removeAttr('href');
         $("#changeWelcomeMessage").attr("data-rel", '');
         $("#sliderYesNo").selectmenu("refresh");
     });

     $("#changeWelcomeMessage").click(function (event) {
         var a = 0;
         if ($("#sliderYesNo")[0].selectedIndex == 0) {
             a = 1;
         }
         if ($("#sliderYesNo")[0].selectedIndex == 1) {
             a = 2;
             var t = $("textarea").val().replace(/\n\r?/g, '<br />');
             var link = "http://10.111.124.47:8000//YaharaEmployeeStatusService/Json/WelcomeMessage?message=" + t + "&callback=?";

             $.getJSON(link)
                .success(
                 function (data) {
                     if (data == true)
                         ; //alert("All Done!");
                     else
                         ;  //alert("Something is not right!");
                 })
                .error(
                 function () {
                     //alert("Writing to Yahara HDTV display failed. Check your connection!");
                 })
                .complete(
                 function () {
                 });

         }
     });

     //     $("#popupLoginButton").click(function (event) {
     //         if (($("un").value == "test") && ($("pass").value == "test")) {
     //             //$('#popupLogin').dialog('close');
     //             //             $.mobile.loadPage('#standupPage',
     //             //                        { transition: "slideup",
     //             //                            reloadPage: true
     //             //                        });
     //             $.mobile.changePage('#standupPage', { transition: "slideup" });
     //         }
     //         else {
     //             event.preventDefault();
     //         }
     //     });

     //$.mobile.changePage('#popupLogin', 'pop', true, true);

     $('#detailPage').live('pageshow', function (event) {
         var i = getParameterByName('item');
         //alert(i);

         var dataDisplayDate = sessionStorage.getItem('data' + 'DisplayDate');
         var dataiDisplayTime = sessionStorage.getItem('data' + i + 'DisplayTime');
         var dataiName = sessionStorage.getItem('data' + i + 'Name');
         var dataiSubject = sessionStorage.getItem('data' + i + 'Subject');
         var dataiMessageHTMLBody = sessionStorage.getItem('data' + i + 'MessageHTMLBody');

         var row =
                '<li data-role="list-divider" id="dateHeader">' + dataDisplayDate
                    + '<span class="ui-li-count" id="timeSent">' + dataiDisplayTime
                    + '</span></li>' +
                '<li>' +
                '<h3> From : ' + dataiName + '</h3>' +
                '<p><strong> Subject : ' + dataiSubject + '</strong></p>' +
                '<p>' + dataiMessageHTMLBody + '</p>' +
                '</li>';
         $("#detailPageul").empty();
         $("#detailPageul").append(row);
         $("#detailPageul").listview('refresh');

     });

     //     $('#tpDateBox').datepicker().onchange(function (event) {
     //         var d = $('#tpDateBox').value;
     //     });
     //     $("#tpDateBox").datebox({
     //         onselect: function (dateText) {
     //             //display("Selected date: " + dateText + "; input's current value: " + this.value);
     //             alert("Selected date: " + dateText + "; input's current value: " + this.value);
     //         }
     //     });

 });

 function sliderValueChange(selectObj) {
     if (selectObj.selectedIndex == 0) {
         $("#changeWelcomeMessage").removeAttr('href');
         $("#changeWelcomeMessage").fadeTo(500, 0.5);
         $("#changeWelcomeMessage").attr("data-rel", '');
     }
     if (selectObj.selectedIndex == 1) {
         $("#changeWelcomeMessage").fadeTo(500, 1.0);
         $("#changeWelcomeMessage").attr("href", '#frondeskPage');
         $("#changeWelcomeMessage").attr("data-rel", 'back');
     }
 }


 function getParameterByName(name) {
     var hash = document.location.hash;
     var match = "";
     if (hash != undefined && hash != null && hash != '') {
         match = RegExp('[?&]' + name + '=([^&]*)').exec(hash);
     }
     else {
         match = RegExp('[?&]' + name + '=([^&]*)').exec(document.location.search);
     }
     return match && decodeURIComponent(match[1].replace(/\+/g, ' '));
 }


// function nl2br_js(myString) {
//     var regX = /\n/gi;

//     s = new String(myString);
//     s = s.replace(regX, "<br /> \n");
//     return s;
 // }

 function tpDateValueSet(obj) {
     var dateChosen = obj.value;
     var dateInNeededFormat = dateChosen.substring(5, 7) + dateChosen.substring(8, 10) + dateChosen.substring(0, 4);
     //localTPDateVariable = new Date(dateChosen.substring(0, 4), dateChosen.substring(5, 7), dateChosen.substring(8, 10),0,0,0);
     localTPDateVariable = new Date();
     localTPDateVariable.setDate(dateChosen.substring(8, 10));
     localTPDateVariable.setMonth(dateChosen.substring(5, 7)-1);
     localTPDateVariable.setFullYear(dateChosen.substring(0, 4))
     callGetEmployeeTargetProcessSummary(dateInNeededFormat);
 }

 function callGetEmployeeTargetProcessSummary(selectedDate) {

     //Clear list
     $("#tpPageList").empty();

     //Add the 'Loading' header
     var row = '<li data-role="list-divider">' + 'Contacting TP Server....' + '</li>';
     $("#tpPageList").append(row);

     $("#tpPageList").listview('refresh');

     var link = "http://10.111.124.47:8000//YaharaEmployeeStatusService/Json/GetEmployeeTargetProcessSummary?date=" + selectedDate + "&callback=?"
     //var link = "http://localhost:51635/YaharaEmployeeStatusService.svc/Json/GetStatus?date=" + selectedDate + "&callback=?"
     //var link = "http://localhost:8000/YaharaEmployeeStatusService/Json/GetStatus?date=" + selectedDate + "&callback=?"
     //var link = "http://atulc-e6500-pc/Where/YaharaEmployeeStatusService.svc/Json/GetStatus?date=" + selectedDate + "&callback=?"
     //var link = "http://atulc-e6500-pc/Where/YaharaEmployeeStatusService.svc/Json/GetStatus?date=" + selectedDate + "&callback=?"
     //var link = "http://localhost:51635/YaharaEmployeeStatusService.svc/Json/GetStatus?date=" + selectedDate + "&callback=?"
     //var link = "http://192.168.1.107/Where/YaharaEmployeeStatusService.svc/Json/GetStatus?date=" + selectedDate + "&callback=?"


     $.getJSON(link)
    .success(
     function (data) {

         $("ul").fadeTo(0, 0.0);

         //Log
         console.log(data.Date);

         //Clear list
         $("#tpPageList").empty();

         //Add the header
         var row = '<li data-role="list-divider" id="dateHeader">'
                    + data.DisplayDate
                    + '<span class="ui-li-count" id="itemCount">' + 'Hours for the week' //data.ListOfItems.length
                    + '</span></li>'
         $("#tpPageList").append(row);

         for (var i = 0; i < data.ListOfItems.length; i++) {
             console.log(data.ListOfItems[i].Email);

             //Each item looks like this    
//                <li>Inbox <span class="ui-li-count">12</span></li>

//             var row = '<li>'
//             + '<img src="http://tp.yaharasoftware.com/avatar.ashx?UserId='
//             + data.ListOfItems[i].Id
//             + '"/>'
//             + '<p>'
//             + data.ListOfItems[i].Name
//             + '</p>'
//             + '<span class="ui-li-count">'
//             + data.ListOfItems[i].TotalHoursLogged 
             //             + '</span></li>';

             var row = '<li>'
             + data.ListOfItems[i].Name
             + '<span class="ui-li-count">'
             + data.ListOfItems[i].TotalHoursLogged
             + '</span></li>';

             $("#tpPageList").append(row);
         }

         $("#tpPageList").listview('refresh');
         $("ul").fadeTo(100, 0.1);
         $("ul").fadeTo(500, 1.0);
     })

    .error(
     function () {
         //Clear list
         $("#tpPageList").empty();

         //Add the header
         var row = '<li data-role="list-divider">' + 'Err! Please try again later.' + '</li>';
         $("#tpPageList").append(row);

         $("#tpPageList").listview('refresh');
     })

    .complete(
     function () {
         //alert("complete");
     });
 }


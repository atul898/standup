
//http://atulc-e6500-pc/Where/YaharaEmployeeStatusService.svc/Json/WelcomeMessage?message=%22Hello%20World!%22


function callService(selectedDate) {

    //Clear list
    $("#messageList").empty();

    //Add the 'Loading' header
    var row = '<li data-role="list-divider">' + 'Contacting outlook server....' + '</li>';
    $("#messageList").append(row);

    $("#messageList").listview('refresh');

    var link = "http://localhost:51635/YaharaEmployeeStatusService.svc/Json/GetStatus?date=" + selectedDate + "&callback=?"
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


 $(document).ready(function () {

     localDateVariable = new Date();

     var curr_date = localDateVariable.getDate();
     var curr_month = localDateVariable.getMonth();
     var curr_year = localDateVariable.getFullYear();
     var selectedDate =
                ("0" + (localDateVariable.getMonth() + 1)).slice(-2) + "" +
                ("0" + localDateVariable.getDate()).slice(-2) + "" +
                localDateVariable.getFullYear();
     callService(selectedDate);


     $("#mainPage").live('swipeleft swiperight', function (event) {
         if (event.type == "swiperight") {
             localDateVariable = localDateVariable.addDays(-1);
             var curr_date = localDateVariable.getDate();
             var curr_month = localDateVariable.getMonth();
             var curr_year = localDateVariable.getFullYear();
             var selectedDate =
                        ("0" + (localDateVariable.getMonth() + 1)).slice(-2) + "" +
                        ("0" + localDateVariable.getDate()).slice(-2) + "" +
                        localDateVariable.getFullYear();
             callService(selectedDate);

             //             var prev = $("#previndex", $.mobile.activePage);
             //             var previndex = $(prev).data("index");
             //             if (previndex != '') {
             //                 $.mobile.changePage({ url: "index.php", type: "get", data: "index=" + previndex }, "slide", true);
             //             }
         }
         if (event.type == "swipeleft") {
             localDateVariable = localDateVariable.addDays(1);
             var curr_date = localDateVariable.getDate();
             var curr_month = localDateVariable.getMonth();
             var curr_year = localDateVariable.getFullYear();
             var selectedDate =
                        ("0" + (localDateVariable.getMonth() + 1)).slice(-2) + "" +
                        ("0" + localDateVariable.getDate()).slice(-2) + "" +
                        localDateVariable.getFullYear();
             callService(selectedDate);

             //             var next = $("#nextindex", $.mobile.activePage);
             //             var nextindex = $(next).data("index");
             //             if (nextindex != '') {
             //                 $.mobile.changePage({ url: "index.php", type: "get", data: "index=" + nextindex });
             //             }
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

          $("#textarea").keyup(function () {
              $("#output_div").html($(this).val().replace('\n', '<br/>'));
          });


//          $("#textarea").keyup(function () {
//              $("#output_div").html(nl2br_js($(this).val()));
//          }); 

     $("#changeWelcomeMessage").click(function (event) {
         var a = 0;
         if ($("#sliderYesNo")[0].selectedIndex == 0) {
             a = 1;
         }
         if ($("#sliderYesNo")[0].selectedIndex == 1) {
             a = 2;

//             var myTextareaVal = $('#textarea').val();
//             var myLineBreak = myTextareaVal.replace(/([^>\r\n]?)(\r\n|\n\r|\r|\n)/g, '<br />');

             //http://atulc-e6500-pc/Where/YaharaEmployeeStatusService.svc/Json/WelcomeMessage?message=%22Hello%20World!%22
             //var link = "http://localhost:51635/YaharaEmployeeStatusService.svc/Json/WelcomeMessage?message=" + $("textarea").val();
             var link = "http://localhost:51635/YaharaEmployeeStatusService.svc/Json/WelcomeMessage";
             //             $.getJSON(link)
             //            .success(
             //             function (data) {


             //             })

             //            .error(
             //             function () {
             //                 alert("Writing to Yahara HDTV display failed. Check your connection!");
             //             })

             //            .complete(
             //             function () {
             //             });

             $.ajax({
                 type: "GET",
                 url: link,
                 data: "message=" + $("textarea").val(),
                 cache: false,
                 dataType: "html",
                 success: function (data) {
                     alert("done!");
                 },
                 error:function () {
                     alert("Writing to Yahara HDTV display failed. Check your connection!");
                 }
             });

         }
     });

     $("#popupLoginButton").click(function (event) {
         if (($("un").value == "test") && ($("pass").value == "test")) {
             //$('#popupLogin').dialog('close');
             //             $.mobile.loadPage('#mainPage',
             //                        { transition: "slideup",
             //                            reloadPage: true
             //                        });
             $.mobile.changePage('#mainPage', { transition: "slideup" });
         }
         else {
             event.preventDefault();
         }
     });

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

 });

 function sliderValueChange(selectObj) {
     if (selectObj.selectedIndex == 0) {
         $("#changeWelcomeMessage").removeAttr('href');
         $("#changeWelcomeMessage").fadeTo(500, 0.5);
         $("#changeWelcomeMessage").attr("data-rel", '');
     }
     if (selectObj.selectedIndex == 1) {
         $("#changeWelcomeMessage").fadeTo(500, 1.0);
         $("#changeWelcomeMessage").attr("href", '#settingsPage');
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
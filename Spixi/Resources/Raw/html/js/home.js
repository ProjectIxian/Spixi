var searchingContacts = false;
var timeoutId = 0;
var balance = "0";

var homeModal = document.getElementById('homeMenuModal');

function onboardingStart()
{
    onboardingLocalizedSkip = SL_Onboarding["onboardingSkip"];
    onboardingLocalizedFinish = SL_Onboarding["onboardingFinish"];
    createOnboardingFrame();
    onboarding(100);
}

function onboarding(section)
{
    var title = "";
    var text = "";
    var showPager = true;
    var showSkip = true;
    switch(section)
    {
        case 1:
            title = SL_Onboarding["screen1Title"];
            text = SL_Onboarding["screen1Text"];
            break;
        case 2:
            title = SL_Onboarding["screen2Title"];
            text = SL_Onboarding["screen2Text"];
            break;
        case 3:
            title = SL_Onboarding["screen3Title"];
            text = SL_Onboarding["screen3Text"];
            break;
        case 4:
            title = SL_Onboarding["screen4Title"];
            text = SL_Onboarding["screen4Text"];
            showSkip = false;
            break;
        case 100:
            title = SL_Onboarding["botTitle"];
            text = SL_Onboarding["botText"];
            text += '<div class="spixi-holder-20"></div>';
            text += '<div class="spixi-separator-white"></div>';
            text += '<div class="spixi-holder-20"></div>';
            text += "<div class=\"intro-button center\" onclick=\"location.href='ixian:joinBot'; onboarding(1);\">" + SL_Onboarding["botJoin"] + "</div>";
            text += '<div class="spixi-holder-20"></div>';
            text += "<div class=\"intro-button center\" onclick=\"onboarding(1);\">" + SL_Onboarding["botDontJoin"] + "</div>";
            showPager = false;
            showSkip = false;
            break;
    }
	setOnboardingContents(title, text, section, showPager, showSkip);
}

if(SL_Onboarding["OnboardingComplete"] == "false")
{
    onboardingStart();
}


function htmlEscape(str) {
    return str
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/\//g, '&#x2F;');
}

function onMainMenuAction() {
    leftSidebar.style.display = "block";
}

function onMainMenuClose() {
    leftSidebar.style.display = "none";
}

function onHomeMenuAction()
{
    homeModal.style.display = "block";
}

function onHomeMenuClose()
{
    homeModal.style.display = "none";
}

// Handle modals outside tap
window.onclick = function(event) {
    if (event.target == homeModal) {
        onHomeMenuClose();
    }
    else if (event.target == leftSidebar) {
        leftSidebar.style.display = "none";
    }
}

function setVersion(version) {
    document.getElementById("version").innerHTML = version;
}

$('#status_balance').on('mousedown touchstart', function() {
    timeoutId = setTimeout(toggleBalance, 1000);
}).on('mouseup mouseleave touchend', function() {
    clearTimeout(timeoutId);
});

$('#wallet_balance').on('mousedown touchstart', function() {
    timeoutId = setTimeout(toggleBalance, 1000);
}).on('mouseup mouseleave touchend', function() {
    clearTimeout(timeoutId);
});


// Toolbar
document.getElementById("MainMenu").onclick = function() {
    onMainMenuAction();
}

document.getElementById("exp1").onclick = function() {
    var x = document.getElementById("exp1-contents");
    var eleft = document.getElementById("exp1-left");
    var eicon = document.getElementById("exp1-icon");



    if (x.style.display === "none")
    {
        x.style.display = "block";
        eleft.className = "spixi-expansion-left active";
        eicon.className = "fa fa-minus";
    }
    else
    {
        x.style.display = "none";
        eleft.className = "spixi-expansion-left";
        eicon.className = "fa fa-caret-down";

    }

}

document.getElementById("exp2").onclick = function() {
    var x = document.getElementById("exp2-contents");
    var eleft = document.getElementById("exp2-left");
    var eicon = document.getElementById("exp2-icon");



    if (x.style.display === "none")
    {
        x.style.display = "block";
        eleft.className = "spixi-expansion-left active";
        eicon.className = "fa fa-minus";
    }
    else
    {
        x.style.display = "none";
        eleft.className = "spixi-expansion-left";
        eicon.className = "fa fa-caret-down";

    }

}

document.getElementById("exp3").onclick = function() {
    var x = document.getElementById("exp3-contents");
    var eleft = document.getElementById("exp3-left");
    var eicon = document.getElementById("exp3-icon");



    if (x.style.display === "none")
    {
        x.style.display = "block";
        eleft.className = "spixi-expansion-left active";
        eicon.className = "fa fa-minus";
    }
    else
    {
        x.style.display = "none";
        eleft.className = "spixi-expansion-left";
        eicon.className = "fa fa-caret-down";

    }

}


function setBalance(theText, theNick) {
    var balDiv = document.getElementById('activity_balance_number');
    var balDiv2 = document.getElementById('wallet_balance_number');

    balance = "<img class=\"ixicash-icon\" src=\"img/ixicash.svg\"/> <span>" + theText + "</span>";

    if(balDiv.innerHTML != "&nbsp;")
        balDiv.innerHTML = balance;

    if(balDiv2.innerHTML != "&nbsp;")
        balDiv2.innerHTML = balance;

    // Set the user nickname
    document.getElementById('menu_nickname').innerHTML = theNick;
}

function toggleBalance()
{
    var balDiv = document.getElementById('activity_balance_number');
    if(balDiv.innerHTML == "&nbsp;")
    {
        balDiv.innerHTML = balance;
        document.getElementById('wallet_balance_number').innerHTML = balance;
        document.getElementById('activity_balance_info').innerHTML = SL_IndexBalanceInfo;
        document.getElementById('wallet_balance_info').innerHTML = SL_IndexBalanceInfo;

    }
    else
    {
        balDiv.innerHTML = "&nbsp;";
        document.getElementById('wallet_balance_number').innerHTML = "&nbsp;";
        document.getElementById('activity_balance_info').innerHTML = SL_IndexBalanceInfo2;
        document.getElementById('wallet_balance_info').innerHTML = SL_IndexBalanceInfo2;

    }
}

function selectFABOption(link) {
    onHomeMenuClose();
    location.href = "ixian:" + link;
}

function selectMenuOption(link) {
    onMainMenuClose();
    location.href = "ixian:" + link;
}

function loadAvatar(avatar_path) {
    avatar_path = avatar_path.replace(/&#92;/g, '\\');

    var av_path = avatar_path + "?t=" + new Date().getTime();
    document.getElementById("avatar_image").src = av_path;
}

// Filter in the CONTACTS tab
function contactSearch()
{
    var a,b,i,row;
    var input = document.getElementById('contactInput');
    var filter = input.value.toUpperCase();
    var c_contactlist = document.getElementById("contactslist");
    var c_items = c_contactlist.getElementsByClassName('spixi-list-item');

    if (isBlank(filter) == true) {
        searchingContacts = false;
    }
    else {
        searchingContacts = true;
    }

    // Go through each element and filter out non-matching elements
    for (i = 0; i < c_items.length; i++) {
        row = c_items[i].getElementsByTagName("div")[0];
        b = row.getElementsByTagName("div")[2];
        a = b.getElementsByTagName("div")[0];

        if (a.innerHTML.toUpperCase().indexOf(filter) > -1) {
            c_items[i].style.display = "";
        } else {
            c_items[i].style.display = "none";
        }
    }
}

// Clears all contacts from contacts page
function clearContacts()
{
    if (searchingContacts == true) {
        return;
    }

    var contactsNode = document.getElementById("contactslist");
    while (contactsNode.firstChild) {
        contactsNode.removeChild(contactsNode.firstChild);
    }
}

// Adds a contact to the contacts page
function addContact(wal, name, avatar, online, unread)
{
    name = htmlEscape(name);
    avatar = avatar.replace(/&#92;/g, '\\');

    if (searchingContacts == true) {
        return;
    }

    var indicator = " offline";
    if (online == "true") {
        indicator = " online";
    }

    var unreadIndicator = "";
    if (unread > 0) {
        unreadIndicator = " unread";
    }

    var contactsNode = document.getElementById("contactslist");

    var contactEntry = document.createElement("div");
    contactEntry.id = "c_" + wal;
    contactEntry.className = "spixi-list-item" + indicator + unreadIndicator;
    contactEntry.innerHTML = '<a href="ixian:details:' + wal + '"><div class="row"><div class="col-2 spixi-list-item-left"><img class="spixi-list-item-avatar" src="' + avatar + '"/><div class="spixi-friend-status-indicator"></div></div><div class="col-8 spixi-list-item-center"><div class="spixi-list-item-title-center nick">' + name + '</div></div><div class="col-2 spixi-list-item-right"><div class="spixi-chat-unread-indicator"></div></div></div></a>';

    contactsNode.appendChild(contactEntry);
}

function setContactStatus(wal, online, unread, excerpt, msgTimestamp)
{
    // ipdate for contacts
    var el = document.getElementById("c_" + wal);
    if(el == null)
    {
        return;
    }
    
    var indicator = " offline";
    if (("" + online).toLowerCase() == "true") {
        online = "true";
        indicator = " online";
    }

    var unreadIndicator = "";
    if (unread > 0) {
        unreadIndicator = " unread";
    }
    el.className = "spixi-list-item" + indicator + unreadIndicator;
    
    // update for chats
    var chatEl = document.getElementById("ch_" + wal);
    var unreadEl = document.getElementById("un_" + wal);
    if(chatEl != null)
    {
        if((excerpt == "" && msgTimestamp == 0) || chatEl.getElementsByClassName("excerpt")[0].innerHTML == excerpt)
        {
            chatEl.className = "spixi-list-item" + indicator + unreadIndicator;
            if(unreadEl != null)
            {
                unreadEl.className = "spixi-list-item" + indicator + unreadIndicator;
            }
            return;
        }
        chatEl.parentElement.removeChild(chatEl);
    }

    if(unreadEl != null)
    {
        unreadEl.parentElement.removeChild(unreadEl);
    }

    var nickEl = el.getElementsByClassName("nick");
    var nick = nickEl[0].innerHTML;
    var avatarEl = el.getElementsByClassName("spixi-list-item-avatar");
    var avatarSrc = avatarEl[0].src;

    addChat(wal, nick, msgTimestamp, avatarSrc, online, excerpt, unread, true);

    if(unread > 0)
    {
        addUnreadActivity(wal, nick, msgTimestamp, avatarSrc, online, excerpt, true);
    }
}


// Clears payment activity from wallet page
function clearPaymentActivity()
{
    var paymentsNode = document.getElementById("paymentlist");
    while (paymentsNode.firstChild) {
        paymentsNode.removeChild(paymentsNode.firstChild);
    }
}

// Adds a payment
function addPaymentActivity(txid, text, timestamp, amount, confirmed)
{
    var iconClass = "spixi-text-red";
    var icon = '<i class="fa fa-circle-notch fa-spin"></i>';
    if (confirmed == "true") {
        iconClass = "spixi-text-green";
        icon = '<i class="fa fa-check-circle"></i>';
    }else if(confirmed == "error")
    {
        iconClass = "spixi-text-red";
        icon = '<i class="fa fa-exclamation-circle"></i>';
    }

    var paymentsNode = document.getElementById("paymentlist");

    var paymentEntry = document.createElement("div");
    paymentEntry.className = "spixi-list-item payment";
    paymentEntry.innerHTML = '<a href="ixian:txdetails:' + txid + '"><div class="row"><div class="col-2 spixi-list-item-left"><div class="' + iconClass + '">' + icon + '</div></div><div class="col-6 spixi-list-item-center"><div class="spixi-list-item-title">' + text + '</div><div class="spixi-list-item-subtitle">' + amount + '</div></div><div class="col-4 spixi-list-item-right"><div class="spixi-timestamp">' + timestamp + '</div></div></div></a>';

    paymentsNode.appendChild(paymentEntry);
}

// Clears all chats from chats page
function clearChats() {
    document.getElementById("chatlist").innerHTML = "";
}

// Adds a chat
function addChat(wallet, from, timestamp, avatar, online, excerpt_msg, unread, insertToTop)
{
    from = htmlEscape(from);
    timestamp = htmlEscape(timestamp);
    avatar = avatar.replace(/&#92;/g, '\\');
    excerpt_msg = htmlEscape(excerpt_msg);

    var excerpt = excerpt_msg;

    var indicator = " offline";
    if (online == "true") {
        indicator = " online";
    }

    var unreadIndicator = "";
    if (unread > 0) {
        unreadIndicator = " unread";
    }

    var timeClass = "spixi-timestamp";
    var relativeTime = getRelativeTime(timestamp);

    if (getTimeDifference(timestamp) < 3600) {
        timeClass = "spixi-timestamp spixi-rel-ts-active";
    }

    var chatsNode = document.getElementById("chatlist");
    var readmsg = document.createElement("div");
    readmsg.id = "ch_" + wallet;
    readmsg.className = "spixi-list-item" + indicator + unreadIndicator;
    readmsg.href = "ixian:chat:" + wallet;
    readmsg.innerHTML = '<a href="ixian:chat:' + wallet + '"><div class="row"><div class="col-2 spixi-list-item-left"><img class="spixi-list-item-avatar" src="' + avatar + '"/><div class="spixi-friend-status-indicator"></div></div><div class="col-6 spixi-list-item-center"><div class="spixi-list-item-title">' + from + '</div><div class="spixi-list-item-subtitle excerpt">' + excerpt + '</div></div><div class="col-4 spixi-list-item-right"><div class="spixi-chat-unread-indicator"></div><div class="' + timeClass + '" data-timestamp="' + timestamp + '">' + relativeTime + '</div></div></div></a>';

    if(insertToTop)
    {
        chatsNode.insertBefore(readmsg, chatsNode.firstElementChild);
    }else
    {
        chatsNode.appendChild(readmsg);
    }
    document.getElementById("chatlist").style.display = 'block';
    document.getElementById("chat_no_activity").style.display = 'none';
}

function setUnreadIndicator(unread_count) {
    if (unread_count != "0") {
        document.getElementById("tab2").firstElementChild.className = "spixi-tab-pad unread";
    } else {
        document.getElementById("tab2").firstElementChild.className = "spixi-tab-pad";
    }
    document.getElementById("unread_count").innerHTML = unread_count;
}

// Clears all unread activity from main page
function clearUnreadActivity() {
    document.getElementById("exp2-contents").innerHTML = "";
}

function addUnreadActivity(wallet, from, timestamp, avatar, online, excerpt_msg, insertToTop) {
    from = htmlEscape(from);
    timestamp = htmlEscape(timestamp);
    avatar = avatar.replace(/&#92;/g, '\\');
    excerpt_msg = htmlEscape(excerpt_msg);

    var excerpt = excerpt_msg;

    var indicator = " offline";
    if (online == "true") {
        indicator = " online";
    }

    var timeClass = "spixi-timestamp";
    var relativeTime = getRelativeTime(timestamp);

    if (getTimeDifference(timestamp) < 3600) {
        timeClass = "spixi-timestamp spixi-rel-ts-active";
    }

    var chatsNode = document.getElementById("exp2-contents");
    var readmsg = document.createElement("div");
    readmsg.href = "ixian:chat:" + wallet;
    readmsg.id = "un_" + wallet;
    readmsg.className = "spixi-list-item " + indicator;
    readmsg.innerHTML = '<a href="ixian:chat:' + wallet + '"><div class="row"><div class="col-2 spixi-list-item-left"><img class="spixi-list-item-avatar" src="' + avatar + '"/><div class="spixi-friend-status-indicator"></div></div><div class="col-6 spixi-list-item-center"><div class="spixi-list-item-title">' + from + '</div><div class="spixi-list-item-subtitle">' + excerpt + '</div></div><div class="col-4 spixi-list-item-right"><div class="' + timeClass + '" data-timestamp="' + timestamp + '">' + relativeTime + '</div></div></div></a>';

    if(insertToTop)
    {
        chatsNode.insertBefore(readmsg, chatsNode.firstElementChild);
    }else
    {
        chatsNode.appendChild(readmsg);
    }
    document.getElementById("chatlist").style.display = 'block';
    document.getElementById("chat_no_activity").style.display = 'none';
}

function setNotificationCount(notification_count) {
    document.getElementById("notification_count").innerHTML = notification_count;
}

// Function to toggle tab's active color
$('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
    // Not very elegant, but it works
    document.getElementById("tab1").className = "col-3 spixi-tab";
    document.getElementById("tab2").className = "col-3 spixi-tab";
    document.getElementById("tab3").className = "col-3 spixi-tab";
    document.getElementById("tab4").className = "col-3 spixi-tab";

    var cl = "active";
    e.target.parentElement.className = "col-3 spixi-tab " + cl;

    var tabid = e.target.parentElement.id;
    location.href = "ixian:tab:" + tabid;
});



// Handle sidebar swiping
$("#leftSidebarHelper").swipe( {
    swipeStatus:function(event, phase, direction, distance)
    {
        var str = "";
        if(direction == "right")
        {
            leftSidebar.style.display = "block";
            document.getElementById('leftSidebarHelper').style.display = "none";

            leftSidebar.style.right = "0px";
            document.getElementById("version").style.left = "0px";
        }
    },
    threshold:10
});

$("#leftSidebar").swipe( {
    swipeStatus:function(event, phase, direction, distance)
    {
        if(direction == "left")
        {
            if (phase=="move")
            {
                leftSidebar.style.left = "-" + distance + "px";
                leftSidebar.style.right = distance + "px";
                document.getElementById("version").style.left = "-" + distance + "px";
            }

            if (phase == "end" || phase == "cancel")
            {
                if(distance > 100)
                {
                    leftSidebar.style.display = "none";
                    document.getElementById('leftSidebarHelper').style.display = "block";
                }
                leftSidebar.style.left = "0px";
                leftSidebar.style.right = "0px";
                document.getElementById("version").style.left = "0px";
            }
        }
    },
    triggerOnTouchEnd:true,
    threshold:100,
    allowPageScroll:"vertical"
});



function selectTab(tab) {
    $("#" + tab + " > a").tab('show');
}


var logoClicked = 0;

function countLogoClick()
{
    logoClicked++;
    if(logoClicked > 10)
    {
        document.getElementById("SendLogMenuItem").style.display = "block";
        alert(SL_DevMode);
    }
}
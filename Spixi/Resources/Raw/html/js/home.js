var searchingContacts = false;
var timeoutId = 0;
var balance = "0";
var fiatBalance = "0";
var hideBalance = false;

var homeModal = document.getElementById('homeMenuModal');

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

document.getElementById("activity_balance_toggle").onclick = function () {
    toggleBalance();
}

// Toolbar
document.getElementById("MainMenu").onclick = function() {
    onMainMenuAction();
}

document.getElementById("filter-all").onclick = function () {
    location.href = "ixian:filter:all";
}
document.getElementById("filter-sent").onclick = function () {
    location.href = "ixian:filter:sent";
}
document.getElementById("filter-received").onclick = function () {
    location.href = "ixian:filter:received";
}

function displayBalance(hideBalance) {
    const balanceElement = document.getElementById('activity_balance_number');
    const fiatBalanceElement = document.getElementById('activity_balance_info');
    const toggleElement = document.getElementById('activity_balance_toggle');

    if (!hideBalance) {
        balanceElement.innerHTML = balance;
        fiatBalanceElement.innerHTML = fiatBalance;
        toggleElement.innerHTML = SL_IndexBalanceHide + " <i class='fa fa-eye'></i>";
    } else {
        balanceElement.innerHTML = "<img class=\"ixicash-icon\" src=\"img/ixilogo.svg\"/> <span>--</span>";
        fiatBalanceElement.innerHTML = SL_IndexBalanceInfo;
        toggleElement.innerHTML = SL_IndexBalanceShow + " <i class='fa fa-eye-slash'></i>";
    }
}

function setBalance(bal, fiatBal, theNick) {
    // Set the user nickname
    document.getElementById('menu_nickname').innerHTML = theNick;

    bal = amountWithCommas(bal);
    fiatBal = amountWithCommas(fiatBal);
    balance = "<img class=\"ixicash-icon\" src=\"img/ixilogo.svg\"/> <span>" + bal + "</span>";
    fiatBalance = "$" + fiatBal;

    if (hideBalance == false) {
        displayBalance(false);
    }
}

function toggleBalance()
{
    hideBalance = !hideBalance;
    displayBalance(hideBalance);
    location.href = `ixian:balance:${hideBalance ? 'hide' : 'show'}`;
}

function setHideBalance(hide)
{
    hideBalance = hide === "True";
    displayBalance(hideBalance);
}

function selectFABOption(link) {
    onHomeMenuClose();
    location.href = "ixian:" + link;
}

function selectMenuOption(link) {
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
        if((excerpt == "") || chatEl.getElementsByClassName("excerpt")[0].innerHTML == excerpt)
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
function clearPaymentActivity(filter)
{
    var paymentsNode = document.getElementById("paymentlist");
    while (paymentsNode.firstChild) {
        paymentsNode.removeChild(paymentsNode.firstChild);
    }

    updateFilterButton("filter-all", "");
    updateFilterButton("filter-sent", "");
    updateFilterButton("filter-received", "");

    if (filter == "all")
        updateFilterButton("filter-all", "active");
    else if (filter == "sent")
        updateFilterButton("filter-sent", "active");
    else if (filter == "received")
        updateFilterButton("filter-received", "active");       
}

function updateFilterButton(btnid, filter)
{
    document.getElementById(btnid).className = "spixi-activity-filterbutton " + filter;
}

// Adds a payment
function addPaymentActivity(txid, receive, text, timestamp, amount, fiatAmount, confirmed)
{
    document.getElementById("section_transactions").style = "display:block";
    document.getElementById("section_no_transactions").style = "display:none";


    var iconClass = "spixi-text-red";
    var icon = '<i class="fa fa-spinner fa-spin"></i>';
    if (confirmed == "true") {
        iconClass = "spixi-text-green";
        icon = '<i class="fa fa-check-circle"></i>';
    }else if(confirmed == "error")
    {
        iconClass = "spixi-text-red";
        icon = '<i class="fa fa-exclamation-circle"></i>';
    }


    var arrow = '<i class="spixi-list-tx-icon spixi-tx-green fa fa-arrow-down"></i>';

    amount = amountWithCommas(amount);

    var amountText = "+ " + amount;
    if (receive == "0") {
        arrow = '<i class="spixi-list-tx-icon spixi-tx-red fa fa-arrow-up"></i>';
        amountText = "- " + amount;
    }

    fiatAmount = amountWithCommas(fiatAmount);
    var fiatAmountText = "$" + fiatAmount;

    var paymentsNode = document.getElementById("paymentlist");

    var paymentEntry = document.createElement("div");
    paymentEntry.innerHTML = '<a href="ixian:txdetails:' + txid + '"><div class="row no-gutters spixi-list-item-first-row"><div class="col"><div class="spixi-list-item-from"><i class="spixi-list-item-from-status spixi-status-yellow fa fa-spinner fa-spin"></i> ' + text + '</div></div> <div class="col spixi-list-item-right"><div class="spixi-list-item-amount">' + amountText + arrow + '</div></div></div><div class="row no-gutters spixi-list-item-second-row"><div class="col"><div class="spixi-list-item-timestamp">' + timestamp + '</div></div><div class="col spixi-list-item-right"><div class="spixi-list-item-amount-fiat">' + fiatAmountText + '</div></div></div></a>';

    paymentsNode.appendChild(paymentEntry);
}

// Clears all chats from chats page
function clearChats() {
    document.getElementById("chatlist").innerHTML = "";
    document.getElementById("chat_no_activity").style.display = 'block';
    document.getElementById("chat_action_button").style.display = 'none';
}

// Adds a chat
function addChat(wallet, from, timestamp, avatar, online, excerpt_msg, type, unread, insertToTop)
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
    var readIndicator = "";

    switch (type) {
        case "read":
            readIndicator = '<i class="spixi-chat-read-indicator spixi-chat-read-indicator-read fas fa-check"></i>';
            break;
        case "confirmed":
            readIndicator = '<i class="spixi-chat-read-indicator spixi-chat-read-indicator-confirmed fas fa-check"></i>';
            break;
    }

    var excerpt_style = type === "typing" ? "typing" : "";

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

    readmsg.innerHTML = '<a href="ixian:chat:' + wallet + '"><div class="row"><div class="col-2 spixi-list-item-left"><img class="spixi-list-item-avatar" src="' + avatar + '"/><div class="spixi-friend-status-indicator"></div></div><div class="col-6 spixi-list-item-center"><div class="spixi-list-item-title">' + from + '</div><div class="spixi-list-item-subtitle ' + excerpt_style + '">' + excerpt + '</div></div><div class="col-4 spixi-list-item-right"><div class="spixi-chat-unread-indicator"></div>' + readIndicator + '<div class="' + timeClass + '" data-timestamp="' + timestamp + '">' + relativeTime + '</div></div></div></a>';

    if(insertToTop)
    {
        chatsNode.insertBefore(readmsg, chatsNode.firstElementChild);
    }else
    {
        chatsNode.appendChild(readmsg);
    }
    document.getElementById("chatlist").style.display = 'block';
    document.getElementById("chat_no_activity").style.display = 'none';
    document.getElementById("chat_action_button").style.display = 'block';
}

function setUnreadIndicator(unread_count) {
    if (unread_count != "0") {
        document.getElementById("tab2").firstElementChild.className = "spixi-tab-pad unread";
    } else {
        document.getElementById("tab2").firstElementChild.className = "spixi-tab-pad";
    }
}

// Clears all unread activity from main page
function clearUnreadActivity() {

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

    document.getElementById("chatlist").style.display = 'block';
    document.getElementById("chat_no_activity").style.display = 'none';
}

function setNotificationCount(notification_count) {
    document.getElementById("notification_count").innerHTML = notification_count;
}

// Function to toggle tab's active color
$('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
    // Not very elegant, but it works
    document.getElementById("tab1").className = "col-4 spixi-tab";
    document.getElementById("tab2").className = "col-4 spixi-tab";
    document.getElementById("tab3").className = "col-4 spixi-tab";

    var cl = "active";
    e.target.parentElement.className = "col-4 spixi-tab " + cl;

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
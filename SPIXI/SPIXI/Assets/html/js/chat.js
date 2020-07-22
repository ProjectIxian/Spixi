var notifications = false;
var isBot = false;
var isAdmin = false;
var messageCost = "";

var attachMode = false;

var contactrequestbar = document.getElementById("contactrequestbar");

function onChatScreenLoad()
{
    document.getElementById("chat_input").focus();
    twemoji.base = "libs/twemoji/";
    twemoji.size = "72x72";
    messagesEl.oncontextmenu = function(e)
    {
        hideContextMenus();
        displayContextMenu(e);
        e.stopPropagation();
        return false;
	};
    messagesEl.addEventListener("click", function (e) {
        if (e.target.className.indexOf("nick") != -1) {
            var nickEl = e.target;
            var nick = nickEl.getAttribute("nick");
            var address = nickEl.getAttribute("address");
            if (address != nick) {
                if (nickEl.innerHTML == nick) {
                    nickEl.innerHTML = address;
                } else {
                    nickEl.innerHTML = nick;
                }
            }
        }
    });

    document.body.oncontextmenu = function(e)
    {
        hideContextMenus();
        return false;
	};
    document.body.addEventListener("click", function(e){
        hideContextMenus();
	});
    onload();
}

function hideContextMenus()
{
    hideChannelSelector();
    hideContextMenu();
}

function setBotMode(bot, cost, costText, admin, botDescription, notificationsString, address)
{
    if(admin == "True")
    {
         isAdmin = true;
         document.getElementsByClassName("spixi-bot-user-actions")[0].style.display = "block";
	}else
    {
         isAdmin = false;
         document.getElementsByClassName("spixi-bot-user-actions")[0].style.display = "none";
	}

    setBotAddress(address);

    messageCost = cost;
    var payBar = document.getElementById("SpixiPayableBar");
    if(payBar != null)
    {
        document.body.removeChild(payBar);
    }

    if(messageCost != "0.00000000")
    {
        var msgEl = document.createElement("div");
        msgEl.id = "SpixiPayableBar";
        msgEl.className = "spixi-chat-payable-bar";
        msgEl.innerHTML = "<span><i class='fa fa-info-circle'></i></span> " + costText;

        document.body.appendChild(msgEl);
        document.getElementById("chatholder").style.height = "84px";
    }else
    {
        document.getElementById("chatholder").style.height = "60px";    
	}

    if(bot == "True")
    {
        isBot = true;
        document.getElementsByClassName("spixi-toolbar-holder")[0].className = "spixi-toolbar-holder bot";
        document.getElementsByClassName("spixi-channel-bar")[0].style.display = "block";
	}else
    {
        isBot = false;
        document.getElementsByClassName("spixi-toolbar-holder")[0].className = "spixi-toolbar-holder";
        document.getElementsByClassName("spixi-channel-bar")[0].style.display = "none";
	}

    if(notificationsString == "True")
    {
        notifications = true;
        document.getElementsByClassName("spixi-bot-notifications-toggle")[0].className = "spixi-switch spixi-bot-notifications-toggle";
	}else
    {
        notifications = false;
        document.getElementsByClassName("spixi-bot-notifications-toggle")[0].className = "spixi-switch off spixi-bot-notifications-toggle";
	}

    document.getElementsByClassName("spixi-bot-description")[0].innerHTML = botDescription;
}

var selectedChannel = 0;

function setSelectedChannel(id, icon, name)
{
    selectedChannel = id;
    var channelBarEl = document.getElementsByClassName("spixi-channel-bar")[0]; 
    channelBarEl.getElementsByClassName("channel-icon")[0].innerHTML = "<i class=\"fa " + icon + "\"></i>";
    channelBarEl.getElementsByClassName("channel-name")[0].innerHTML = name;
}

function onChatScreenLoaded()
{
    document.getElementById("chatattachbar").style.bottom = -document.getElementById("chatattachbar").offsetHeight + "px";
    $('#chat_emoji').lsxEmojiPicker({
        twemoji: true,
        onSelect: function (emoji) {
            var chatInput = document.getElementById("chat_input");
            if(chatInput.innerHTML == "<br>")
            {
                chatInput.innerHTML = emoji.value + " ";
            }else
            {
                chatInput.innerHTML += emoji.value + " ";
            }
            chatInput.focus();
        }
    });
    document.getElementById("chat_input").focus();
}

document.getElementById("backbtn").onclick = function () {
    if(document.getElementById("ContactsBox"))
    {
        hideContacts();
	}else if(document.getElementById("BotDetails").style.display == "block")
    {
        hideBotDetails();
    }else
    {
        location.href = "ixian:back";
	}
}

function test() {
    setNickname("TesterTesterTesterTesterTesterTesterTesterTesterTesterTesterTesterTester");
    //showContactRequest(true);
    addMe(0, "img/spixiavatar.png", "Hi!", "11:23 AM");
    addFile(10, 9090, "img/spixiavatar.png", "file1.png", "10:23 AM", false, false, false);
    setTimeout(function () { updateFile(9090, "25", "False"); }, 1000);
    setTimeout(function () { updateFile(9090, "50", "False"); }, 2000);
    setTimeout(function () { updateFile(9090, "75", "False"); }, 3000);
    setTimeout(function () { updateFile(9090, "100", "False"); }, 4000);

    setTimeout(function () { addCall(1000, "Incoming call", "Duration 15:32", "False", "11:23 AM"); }, 1000);
    setTimeout(function () { addCall(1000, "Incoming call", "Declined", "True", "11:23 AM"); }, 2000);

    setTimeout(function () { addMe(1, "img/spixiavatar.png", "How are you today? &#x1f602", "11:23 AM", "True", "True"); }, 1000);
    setTimeout(function () { addThem(2, "img/spixiavatar.png", "Hey! &#x1f604", "11:24 AM", "True", "True"); }, 1300);
    setTimeout(function () { addThem(3, "img/spixiavatar.png", "Great, thanks for asking.", "11:24 AM", "True", "True"); }, 1600);
    setTimeout(function () { addThem(4, "img/spixiavatar.png", "And how are you?", "11:24 AM", "True", "True"); }, 1900);
    setTimeout(function () { addFile(12, 12, "", "file.png", "10:27 AM", "True", "True", "True"); }, 2200);
    setTimeout(function () { addMe(5, "", "Great! Just got back from the moon.", "11:24 AM", "True", "True"); }, 2200);
    setTimeout(function () { addThem(6, "img/spixiavatar.png", "Ohh??? What were you doing there?", "11:25 AM", "True", "True"); }, 2500);
    setTimeout(function () { addMe(7, "", "Was building my own Luna Park...", "11:25 AM", "True", "False"); }, 3000);
    setTimeout(function () { addMe(8, "", "html<div>injection</div>test", "11:25 AM", "False", "False"); }, 4000);

    setTimeout(function () { updateMessage(12, "50", "True", "True"); }, 5000);
}


document.getElementById("ca_request").onclick = function () {
    location.href = "ixian:request";
}
document.getElementById("ca_send").onclick = function () {
    location.href = "ixian:send";
}

/*document.getElementById("ca_app").onclick = function () {
    document.getElementById("AppsMenu").style.display = "block";
}*/

document.getElementById("ca_sendfile").onclick = function () {
    var chatInput = document.getElementById("chat_input");
    chatInput.click();
    chatInput.focus();
    if (attachMode == true) {
        attachMode = false;
        hideAttach();
    }

    location.href = "ixian:sendfile";
}

document.getElementById("request_bar_ignore").onclick = function () {
    location.href = "ixian:back";
}
document.getElementById("request_bar_accept").onclick = function () {
    showContactRequest(false);
    location.href = "ixian:accept";
}
function showContactRequest(show) {
    if (show == true) {
        contactrequestbar.style.display = "block";
        document.getElementById("chat_input").disabled = true;
    }
    else {
        contactrequestbar.style.display = "none";
        document.getElementById("chat_input").disabled = false;
    }
}

document.getElementById("chat_send").onclick = function () {
    var chatInput = document.getElementById("chat_input");
    chatInput.click();
    chatInput.focus();
    var chat_text = chatInput.innerText;

    if(chat_text.length > 1000)
    {
        alert(SL_ChatTextTooLong);
        return false;
    }

    if (chat_text.length > 0)
        location.href = "ixian:chat:" + encodeURIComponent(chat_text);
}

var shiftPressed = false;

$("#chat_input").keydown(function (event) {
    if(event.keyCode === 16)
    {
        shiftPressed = true;
    }
});

$("#chat_input").keypress(function (event) {
    if(event.keyCode === 13)
    {
        if(!shiftPressed)
        {
            return false;
        }
    }
});

$("#chat_input").keyup(function (event) {
    if(event.keyCode === 16)
    {
        shiftPressed = false;
    }
    if (event.keyCode === 13 && !shiftPressed) {
        $("#chat_send").click();
    }

    var chat_text = document.getElementById("chat_input").innerHTML;
    if (chat_text.length > 0)
        document.getElementById("chat_send").style.backgroundColor = "#C1185B";
    else
        document.getElementById("chat_send").style.backgroundColor = "#BABABA";
});

function shouldScroll() {
    var el = document.getElementById("wrap");
    if (el.scrollTop >= el.scrollHeight - el.clientHeight - 20) {
        return true;
    }
    return false;
}

$("#chat_input").focus(function (event) {
    if (shouldScroll()) {
        setTimeout(function () {
            document.getElementById("chatholder").scrollIntoView(false);
            // Hack for slow devices
            setTimeout(function () {
                document.getElementById("chatholder").scrollIntoView(false);
            }, 800);
        }, 200);
    }
});


$("#chat_input").click(function (event) {
    if (shouldScroll()) {
        setTimeout(function () {
            document.getElementById("chatholder").scrollIntoView(false);
            // Hack for slow devices
            setTimeout(function () {
                document.getElementById("chatholder").scrollIntoView(false);
            }, 800);
        }, 200);
    }
});

function clearInput() {
    document.getElementById("chat_input").innerHTML = "";
    document.getElementById("chat_send").style.backgroundColor = "#BABABA";
}

function parseImageUrl(text)
{
    var imageServerKey = /https:\/\/[A-Za-z0-9]+\.(tenor|giphy)\.com\/[A-Za-z0-9_\/=%\?\-\.\&]+/;
    var imageServerKeyCheck = /^https:\/\/[A-Za-z0-9]+\.(tenor|giphy)\.com\/[A-Za-z0-9_\/=%\?\-\.\&]+$/;
    for(var imageUrlIndex = text.search(imageServerKey), nextStartPos = 0; imageUrlIndex > -1; imageUrlIndex = text.substring(nextStartPos).search(imageServerKey))
    {
        var imageUrl = text.substring(imageUrlIndex, text.length);
        var endIndex = imageUrl.indexOf(" ");
        if(endIndex == -1)
        {
            endIndex = imageUrl.length;
        }
        imageUrl = imageUrl.substring(0, endIndex);
        if(imageUrl.search(imageServerKeyCheck) > -1)
        {
            var imgHtml = '<img src="' + imageUrl + '"/>';
            text = text.replace(imageUrl, imgHtml);
            nextStartPos = imageUrlIndex + imgHtml.length;
        }else
        {
            nextStartPos = imageUrlIndex + 1;
        }
    }
    return text;
}

var messagesEl = document.getElementById("messages");
var chatHolderEl = document.getElementById("chatholder");

function addReactions(id, reactions)
{
    var msgEl = document.getElementById("msg_" + id);
    if(msgEl == null)
    {
        return;
	}

    var scroll = shouldScroll();

    var reactionsEls = msgEl.getElementsByClassName("reactions");
    var reactionsEl = null;
    if(reactionsEl == null)
    {
        if(reactions == "")
        {
            return;  
		}
        reactionsEl = document.createElement("div");
        reactionsEl.className = "reactions";
        msgEl.appendChild(reactionsEl);
	}else
    {
        reactionsEl = reactionsEls[0];
	}

    reactionsEl.innerHTML = "";
    var reactionArr = reactions.split(";");
    for(var i = 0; i < reactionArr.length; i++)
    {
        if(reactionArr[i] == "")
        {
            continue;  
		}
        if(reactionArr[i].indexOf("tip:") == 0)
        {
            reactionsEl.innerHTML += "<div class=\"reaction\"><img class=\"ixicash-icon\" src=\"img/ixicash.svg\"/>" + reactionArr[i].substring(4) + "</div>";
        }else if(reactionArr[i].indexOf("like:") == 0)
        {
            reactionsEl.innerHTML += "<div class=\"reaction\"><i class=\"fa fa-heart\"></i>" + reactionArr[i].substring(5) + "</div>";
        }
    }

    if(reactionsEl.innerHTML != "")
    {
        msgEl.style.paddingBottom = "48px";
	}else
    {
        reactionsEl.parentNode.removeChild(reactionsEl);
        msgEl.style.paddingBottom = "20px";
	}

    if (scroll) {
        chatHolderEl.scrollIntoView(false);
    }
}

function deleteMessage(id)
{
    var msgEl = document.getElementById("msg_" + id);
    if(msgEl == null)
    {
        return;
	}
    msgEl.parentNode.removeChild(msgEl);
}

function addText(id, address, nick, avatar, text, time, className) {
    text = text.replace(/\n/g, "<br>");

    var textEl = document.createElement('div');
    textEl.className = "text selectable";

    try
    {
        text = parseImageUrl(text);
    }catch(e)
    {
    }
            
    textEl.innerHTML = text;
    twemoji.parse(textEl);


    var timeClass = "spixi-timestamp";
    var relativeTime = getRelativeTime(time);

    if (getTimeDifference(time) < 3600) {
        timeClass = "spixi-timestamp spixi-rel-ts-active";
    }

    var timeEl = document.createElement('div');
    timeEl.setAttribute("data-timestamp", time);
    timeEl.className = "time selectable " + timeClass;
    timeEl.innerHTML = relativeTime;

    var bubbleEl = document.createElement('div');
    bubbleEl.id = "msg_" + id;
    bubbleEl.className = className + "";

    if (nick != "") {
        var nickEl = document.createElement('div');
        if (nick == address) {
            nickEl.setAttribute("name", "a_" + address);
        }
        nickEl.setAttribute("nick", nick);
        nickEl.setAttribute("address", address);
        nickEl.className = "nick selectable";
        nickEl.innerHTML = nick;
        bubbleEl.appendChild(nickEl);
    }

    bubbleEl.appendChild(textEl);
    bubbleEl.appendChild(timeEl);
    bubbleEl.innerHTML += "<i class=\"statusIndicator fas fa-check\"></i>";

    if (avatar != "") {

        avatar = avatar.replace(/&#92;/g, '\\');

        var avatarEl = document.createElement('img');
        avatarEl.className = "avatar selectable";
        avatarEl.src = avatar;
        if(nick == "")
        {
            avatarEl.alt = address;
        }
        bubbleEl.appendChild(avatarEl);
    }

    var scroll = shouldScroll();

    messagesEl.appendChild(bubbleEl);

    if (scroll) {
        chatHolderEl.scrollIntoView(false);
    }
}

function addMe(id, address, nick, avatar, text, time, sent, read) {
    var additionalClasses = "";
    if (sent == "True") {
        additionalClasses = " sent";
    }
    if (read == "True") {
        additionalClasses += " read";
    }
    addText(id, address, nick, avatar, text, time, "spixi-bubble myself" + additionalClasses);
}

function addThem(id, address, nick, avatar, text, time) {
    addText(id, address, nick, avatar, text, time, "spixi-bubble");
}

function addFile(id, address, nick, avatar, fileid, name, time, me, sent, read, progress, complete) {

    var additionalClasses = "";
    if (me == "True") {
        additionalClasses = " myself";
        if (sent == "True") {
            additionalClasses += " sent";
        }
        if (read == "True") {
            additionalClasses += " read";
        }
    }

    var textEl = document.createElement('div');
    textEl.className = "text selectable";
    textEl.innerHTML = name;

    var icon = document.createElement('i');
    icon.className = "fa fa-arrow-down actionicon";

    var iconWrap = document.createElement('div');
    iconWrap.className = "icon-wrap";

    iconWrap.appendChild(icon);

    var linkEl = document.createElement('a');
    linkEl.style.display = "block";
    linkEl.id = "file_" + fileid;

    if (me == "True") {
        linkEl.href = "ixian:openfile:" + fileid;
        icon.className = "fa fa-folder-open actionicon";
        linkEl.appendChild(iconWrap);
        linkEl.appendChild(textEl);
    }
    else {
        linkEl.href = "ixian:acceptfile:" + fileid;
        linkEl.appendChild(textEl);
        linkEl.appendChild(iconWrap);
    }


    addText(id, address, nick, avatar, linkEl.outerHTML, time, "spixi-bubble file" + additionalClasses);

    if (progress == "100") {
        updateFile(fileid, progress, complete);
    }
}

function addCall(id, message, declined, time) {
    var textEl = document.createElement('div');
    textEl.className = "text selectable";
    textEl.innerHTML = message;

    var timeClass = "spixi-timestamp";
    var relativeTime = getRelativeTime(time);
    if (getTimeDifference(time) < 3600) {
        timeClass = "spixi-timestamp spixi-rel-ts-active";
    }

    var timeEl = document.createElement('div');
    timeEl.setAttribute("data-timestamp", time);
    timeEl.className = "time selectable " + timeClass;
    timeEl.innerHTML = relativeTime;
        
    var icon = document.createElement('div');
    icon.className = "fa fa-phone icon";

    var bubbleEl = document.getElementById("call_" + id);
    var append = false;
    if(bubbleEl == null)
    {
        bubbleEl = document.createElement('div');
        append = true;
    }else
    {
        bubbleEl.innerHTML = "";
    }

    bubbleEl.id = "call_" + id;
    bubbleEl.className = "spixi-callbubble";
    if (declined == "True") {
        bubbleEl.className = "spixi-callbubble spixi-callbubble-declined";
        icon.className = "fa fa-phone-slash icon";
    }

    bubbleEl.appendChild(icon);

    var dataEl = document.createElement('div');
    dataEl.className = "spixi-call-data";
    dataEl.appendChild(textEl);
    dataEl.appendChild(timeEl);

    bubbleEl.appendChild(dataEl);


    var scroll = shouldScroll();
    if(append)
    {
        document.getElementById("messages").appendChild(bubbleEl);
    }

    if (scroll) {
        document.getElementById("chatholder").scrollIntoView(false);
    }
}

function updateFile(id, progress, complete) {
    var fileEl = document.getElementById("file_" + id);
    if (fileEl != null) {
        if (complete == "True") {
            fileEl.href = "ixian:openfile:" + id;
        }
        else {
            fileEl.href = "javascript:void(0)";
        }

        var aEls = fileEl.getElementsByClassName("actionicon");
        if (aEls.length == 0)
            aEls = fileEl.getElementsByClassName("actionprogress");

        if (aEls.length > 0) {
            var aEl = aEls[0];
            if (complete == "True") {
                aEl.className = "fa fa-folder-open actionicon";
                aEl.innerHTML = "";
            }
            else {
                aEl.className = "actionprogress";
                aEl.innerHTML = progress + "<span class='smaller'>%</span>";
            }
        }
    }
}

function updateMessage(id, message, sent, read) {
    message = message.replace(/\n/g, "<br>");

    var msgEl = document.getElementById("msg_" + id);
    var isFile = false;
    var isPayment = false;

    if (msgEl != null) {

        var additionalClasses = "";
        if (sent == "True") {
            additionalClasses = " sent";
        }
        if (read == "True") {
            additionalClasses += " read";
        }

        if (msgEl.className.indexOf("spixi-payment-request") > -1) {
            additionalClasses += " spixi-payment-request";
            isPayment = true;
        }

        if (msgEl.className.indexOf("file") > -1) {
            additionalClasses += " file";
            isFile = true;
        }

        msgEl.className = "spixi-bubble myself" + additionalClasses;

        if (isFile == false && isPayment == false) {
            try
            {
                message = parseImageUrl(message);
            }catch(e)
            {
            }
            var textEls = msgEl.getElementsByClassName("text");
            if (textEls.length > 0) {
                var textEl = textEls[0];
                textEl.innerHTML = message;

                twemoji.parse(textEl);
            }
        }
    }
}

function addPaymentRequest(id, txid, address, nick, avatar, title, amount, status, statusIcon, time, localSender, sent, read, enableView) {
    var message = "<div id=\"tx_" + txid + "\" class=\"txid-el\"><div class=\"title\"><i class=\"fas fa-flag\"></i> " + title + "</div>";
    message += "<div class=\"status\">" + SL_ChatStatus + ":<div class=\"content\">" + status + "<i class=\"fas " + statusIcon + "\"></i></div></div>";
    message += "<div class=\"amount\">" + SL_ChatAmount + ":<div class=\"content\"><img class=\"ixicash-icon\" src=\"img/ixicash.svg\"/><span>" + amount + "</span></div></div>";
    var viewStyle = "display:none;";
    if (enableView == "True") {
        viewStyle = "";
    }
    message += "<div class=\"view\" style=\"" + viewStyle + "\" onclick=\"location.href = 'ixian:viewPayment:" + id + "'\"><i class=\"fas fa-search\"></i> " + SL_ChatView + "</div>";

    var additionalClasses = "";
    if (localSender == "True") {
        additionalClasses = " myself";
        if (sent == "True") {
            additionalClasses += " sent";
        }
        if (read == "True") {
            additionalClasses += " read";
        }
    }

    addText(id, address, nick, avatar, message, time, "spixi-bubble spixi-payment-request" + additionalClasses);
}

function updatePaymentRequestStatus(msgId, txid, status, statusIcon, enableView) {
    var el = document.getElementById("msg_" + msgId);

    if (el == null) {
        return;
    }

    var tmpEls = el.getElementsByClassName("txid-el");
    tmpEls[0].id = "tx_" + txid;

    var statusEls = el.getElementsByClassName("status");
    var statusContentEls = statusEls[0].getElementsByClassName("content");
    statusContentEls[0].innerHTML = status + "<i class=\"fas " + statusIcon + "\"></i>";

    var viewEls = el.getElementsByClassName("view");
    if (enableView == "True") {
        viewEls[0].style.display = "";
    } else {
        viewEls[0].style.display = "none";
    }

}

function updateTransactionStatus(txid, status, statusIcon) {
    var el = document.getElementById("tx_" + txid);

    if (el == null) {
        return;
    }

    var statusEls = el.getElementsByClassName("status");
    var statusContentEls = statusEls[0].getElementsByClassName("content");
    statusContentEls[0].innerHTML = status + "<i class=\"fas " + statusIcon + "\"></i>";
}

function setNickname(nick) {
    document.getElementById("title").innerHTML = nick;
    document.getElementById("contactrequesttitle").innerHTML = nick + " " + SL_ChatContactRequest;
}

// Handle 'attach' bar, allowing to send and request IXI
document.getElementById("chat_attach").onclick = function () {
    if (attachMode == true) {
        attachMode = false;
        hideAttach();
    }
    else {
        attachMode = true;
        showAttach();
    }
}

function hideAttach() {
    document.getElementById("chatbar").style.bottom = "0px";
    document.getElementById("chatholder").style.height = "60px";
    document.getElementById("chatattachbar").style.bottom = -document.getElementById("chatattachbar").offsetHeight + "px";
    var payBar = document.getElementById("SpixiPayableBar");
    if(payBar != null)
    {
        payBar.style.bottom = "60px";
        document.getElementById("chatholder").style.height = "88px";
	}
}

function showAttach() {
    var attachBarHeight = document.getElementById("chatattachbar").offsetHeight;
    document.getElementById("chatbar").style.bottom = attachBarHeight + "px";
    document.getElementById("chatholder").style.height = (attachBarHeight + 60) + "px";
    document.getElementById("chatattachbar").style.bottom = "0px";
    var payBar = document.getElementById("SpixiPayableBar");
    if(payBar != null)
    {
        payBar.style.bottom = (attachBarHeight + 60) + "px";
        document.getElementById("chatholder").style.height = (attachBarHeight + 60 + 28) + "px";
	}
    setTimeout(function () {
                document.getElementById("chatholder").scrollIntoView(false);
    }, 400);
}

function setOnlineStatus(status) {
    document.getElementById("status").innerHTML = status;
}

function updateGroupChatNicks(address, nick) {
    var nickEls = document.getElementsByName("a_" + address);
    for (var i = 0; i < nickEls.length; i++) {
        var nickEl = nickEls[i];
        nickEl.name = "";
        nickEl.setAttribute("nick", nick);
        nickEl.innerHTML = nick;
    }
}

function addApp(id, name, icon)
{
    var appsEl = document.getElementById("AppsMenu");

    var el = document.createElement("div");
    el.onclick = function() { appsEl.style.display = "none"; location.href = "ixian:app:" + id; };
    el.className = "spixi-app";
    el.innerHTML = "<img src='" + icon + "'/><br/>" + name;

    appsEl.appendChild(el);
}

function showCallButton()
{
    if(!isBot)
    {
        document.getElementById("CallButton").style.display = "block";
    }
}

function showContacts()
{
    var contactsBox = document.getElementById("ContactsBox");
    if(contactsBox != null)
    {
        document.body.removeChild(contactsBox);
	}
    contactsBox = document.createElement("div");
    contactsBox.id = "ContactsBox";
    contactsBox.className = "container-fluid chat-contacts-box";

    document.body.appendChild(contactsBox);

    location.href = "ixian:loadContacts";
}

function hideContacts()
{
    var contactsBox = document.getElementById("ContactsBox");
    if(contactsBox != null)
    {
        document.body.removeChild(contactsBox);
	}
}

function addContact(address, nick, avatar, role)
{
    var contactsBox = document.getElementById("ContactsBox");
    if(contactsBox == null)
    {
        return;
	}

    var userTemplate = document.getElementsByClassName("user")[0].innerHTML;

    var childEl = document.createElement("div");
    childEl.innerHTML = userTemplate;

    childEl.getElementsByClassName("avatar")[0].innerHTML = "<img src='" + avatar + "'/>";
    childEl.getElementsByClassName("nick")[0].innerHTML = nick;
    if(role == "")
    {
        role = "[DEFAULT]";
	}
            
    contactsBox.appendChild(childEl);
}

function showChannels()
{

}

function showDetails()
{

}

function selectChannel(id)
{
    location.href = "ixian:selectChannel:" + id;
}

var channelSelectorEl = null;
function displayChannelSelector(e)
{
    if(channelSelectorEl != null)
    {
        channelSelectorEl.parentNode.removeChild(channelSelectorEl);
	}
    channelSelectorEl = document.createElement("div");
    channelSelectorEl.className = "container-fluid spixi-channel-selector";
    channelSelectorEl.innerHTML = "<div class='spixi-channel-selector-sep'></div>";

    document.body.appendChild(channelSelectorEl);

    location.href = "ixian:populateChannelSelector";

    e.stopPropagation();

    return false;
}

function addChannelToSelector(id, name, icon)
{
    if(channelSelectorEl == null)
    {
        return;
	}
    var channelTemplate = document.getElementsByClassName("channel-selector-template")[0].innerHTML;

    var childEl = document.createElement("div");
    childEl.innerHTML = channelTemplate;

    childEl.getElementsByClassName("channel-icon")[0].innerHTML = "<i class='fa " + icon + "'></i>";
    childEl.getElementsByClassName("channel-name")[0].innerHTML = name;
    if(id == selectedChannel)
    {
        childEl.getElementsByClassName("channel-name")[0].style.fontWeight = "bold";
	}
            
    childEl.onclick = function(ev)
    {
        selectChannel(id);
        hideChannelSelector();
	};

    channelSelectorEl.appendChild(childEl);
}

function hideChannelSelector()
{
    if(channelSelectorEl == null)
    {
        return;
	}

    channelSelectorEl.parentNode.removeChild(channelSelectorEl);
    channelSelectorEl = null;
}

function clearMessages()
{
    messagesEl.innerHTML = "";
}

function displayContextMenu(e)
{
    var contextMenuEl = document.getElementById("ContextMenu");
    if(contextMenuEl != null)
    {
        contextMenuEl.parentNode.removeChild(contextMenuEl);
	}


    var msgEl = null;
    for(var tmpEl = e.target; tmpEl != messagesEl; tmpEl = tmpEl.parentNode)
    {
        msgEl = tmpEl;
	}

    if(msgEl == null)
    {
         return;
	}

    var localMsg = false;
    if(msgEl.className.indexOf("myself") != -1)
    {
        localMsg = true;
	}

    var menuHtml = "";
    //menuHtml += "<div onclick=\"contextAction('pin', '" +  msgEl.id + "');\"><span class=\"icon\"><i class=\"fa fa-map-pin\"></i></span> " + SL_ContextMenu["pinMessage"] + "</div>";
    //menuHtml += "<div onclick=\"contextAction('copy', '" +  msgEl.id + "');\"><span class=\"icon\"><i class=\"fa fa-quote-right\"></i></span> " + SL_ContextMenu["copyText"] + "</div>";
    if(!localMsg)
    {
        menuHtml += "<div onclick=\"contextAction('tip', '" +  msgEl.id + "');\"><span class=\"icon\"><i class=\"fa fa-wallet\"></i></span> " + SL_ContextMenu["tipUser"] + "</div>";
        menuHtml += "<div onclick=\"contextAction('like', '" +  msgEl.id + "');\"><span class=\"icon\"><i class=\"fa fa-heart\"></i></span> " + SL_ContextMenu["likeMessage"] + "</div>";
        if(isBot)
        {
            menuHtml += "<div onclick=\"contextAction('userInfo', '" +  msgEl.id + "');\"><span class=\"icon\"><i class=\"fa fa-info-circle\"></i></span> " + SL_ContextMenu["userInfo"] + "</div>";
            menuHtml += "<div onclick=\"contextAction('sendContactRequest', '" +  msgEl.id + "');\"><span class=\"icon\"><i class=\"fa fa-user-plus\"></i></span> " + SL_ContextMenu["sendContactRequest"] + "</div>";
        }
    }

    if(isAdmin)
    {
        menuHtml += "<div onclick=\"contextAction('kickUser', '" +  msgEl.id + "');\"><span class=\"icon\"><i class=\"fa fa-user-times\"></i></span> " + SL_ContextMenu["kickUser"] + "</div>";
        menuHtml += "<div onclick=\"contextAction('banUser', '" +  msgEl.id + "');\"><span class=\"icon\"><i class=\"fa fa-user-slash\"></i></span> " + SL_ContextMenu["banUser"] + "</div>";
    }
    if(isAdmin || localMsg)
    {
        menuHtml += "<div onclick=\"contextAction('deleteMessage', '" +  msgEl.id + "');\"><span class=\"icon\"><i class=\"fa fa-trash-alt\"></i></span> " + SL_ContextMenu["deleteMessage"] + "</div>";
	}

    contextMenuEl = document.createElement("div");
    contextMenuEl.id = "ContextMenu";
    contextMenuEl.className = "chat-context-menu";
    contextMenuEl.onclick = function(e)
    {
        e.stopPropagation();
        return false;
	};

    contextMenuEl.innerHTML = menuHtml;
    contextMenuEl.style.left = e.clientX + "px";
    contextMenuEl.style.top = e.clientY + "px";

    document.body.appendChild(contextMenuEl);

    if(contextMenuEl.getBoundingClientRect().bottom > window.innerHeight)
    {
        contextMenuEl.style.top = "auto";
        contextMenuEl.style.bottom = "0px";
        contextMenuEl.style.maxHeight = "400px";
	}

    return;
}

function hideContextMenu()
{
    var contextMenuEl = document.getElementById("ContextMenu");
    if(contextMenuEl != null)
    {
        contextMenuEl.parentNode.removeChild(contextMenuEl);
	}
}

var contextActionMsgId = null;
var tipPrice = "0";

function contextAction(action, msgId)
{
    msgId = msgId.substring(4);
    contextActionMsgId = msgId;
    if(action == "copy")
    {
        // TODO implement
	}else if (action == "tip")
    {
        var msgEl = document.getElementById("msg_" + msgId);
        var nick = msgEl.getElementsByClassName("nick")[0].getAttribute("nick");

        var title = SL_Modals["tipTitle"];
        title = title.replace("{0}", nick);

        tipPrice = "0";
        var html = SL_Modals["tipBody"];

        html += "<div class=\"spixi-modal-tip-item\" onclick=\"selectTip('50');\">50 IXI <i class=\"fa fa-check\"></i></div>";
        html += "<div class=\"spixi-modal-tip-item\" onclick=\"selectTip('100');\">100 IXI <i class=\"fa fa-check\"></i></div>";
        html += "<div class=\"spixi-modal-tip-item\" onclick=\"selectTip('200');\">200 IXI <i class=\"fa fa-check\"></i></div>";
        html += "<div class=\"spixi-modal-tip-item custom\" onclick=\"selectTip();\">" + SL_Modals["tipCustom"] + " <i class=\"fa fa-check\"></i> <input type='text' class='spixi-textfield' onchange='tipPrice = this.value;'/></div>";

        var payBtnHtml = "<div onclick='payTipConfirmation(\"" + msgId + "\");'>" + SL_Modals["payButton"] + "</div>";
        var cancelBtnHtml = "<div onclick='hideModalDialog();'>" + SL_Modals["cancel"] + "</div>";

        showModalDialog(SL_Modals["tipTitle"], html, payBtnHtml, cancelBtnHtml);
    }else if(action == "userInfo")
    {
        var msgEl = document.getElementById("msg_" + msgId);
        var nick = msgEl.getElementsByClassName("nick")[0].getAttribute("nick");
        var address = msgEl.getElementsByClassName("nick")[0].getAttribute("address");
        var avatar = msgEl.getElementsByClassName("avatar")[0].outerHTML;
        showUserDetails(avatar + " " + nick, address);
    }else
    {
        location.href = "ixian:contextAction:" + action + ":" + msgId;
	}
    hideContextMenu();
}

function selectTip(amount)
{
    var modalEl = document.getElementById("SpixiModalDialog");

    var tipItems = modalEl.getElementsByClassName("spixi-modal-tip-item");

    tipItems[0].className = tipItems[1].className = tipItems[2].className = tipItems[3].className = "spixi-modal-tip-item";

    if(amount == "50")
    {
        tipItems[0].className += " selected";
        tipPrice = amount;
        modalEl.getElementsByClassName("spixi-textfield")[0].value = "";
	}else if(amount == "100")
    {
        tipItems[1].className += " selected";
        tipPrice = amount;
        modalEl.getElementsByClassName("spixi-textfield")[0].value = "";
	}else if(amount == "200")
    {
        tipItems[2].className += " selected";
        tipPrice = amount;
        modalEl.getElementsByClassName("spixi-textfield")[0].value = "";
	}else
    {
        tipItems[3].className += " selected";
        if(modalEl.getElementsByClassName("spixi-textfield")[0].value != "")
        {
            tipPrice = modalEl.getElementsByClassName("spixi-textfield")[0].value;
		}else
        {
            tipPrice = "0";  
		}
	}
}

function payTipConfirmation(msgId)
{
    if(tipPrice == "0")
    {
        return;
    }

    var msgEl = document.getElementById("msg_" + msgId);

    var address = msgEl.getElementsByClassName("nick")[0].getAttribute("address");
    var nick = msgEl.getElementsByClassName("nick")[0].getAttribute("nick");

    var title = SL_Modals["tipTitle"];
    title = title.replace("{0}", nick);

    var html = SL_Modals["tipConfirmationBody"];

    html = html.replace("{0}", nick + " (" + address + ")");
    html = html.replace("{1}", tipPrice + " IXI");

    var payBtnHtml = "<div onclick='payTip();'>" + SL_Modals["payButton"] + "</div>";
    var cancelBtnHtml = "<div onclick='hideModalDialog();'>" + SL_Modals["cancel"] + "</div>";

    showModalDialog(title, html, payBtnHtml, cancelBtnHtml);
}

function payTip()
{
    hideModalDialog();
    location.href = "ixian:contextAction:tip:" + contextActionMsgId + ":" + tipPrice;
}

function showBotDetails()
{
    if(!isBot)
    {
        return;
    }
    document.getElementById("BotDetails").style.display = "block";
}

function hideBotDetails()
{
    document.getElementById("BotDetails").style.display = "none";
}

function toggleNotifications(el)
{
    if(notifications)
    {
        notifications = false;
        el.className = "spixi-switch off spixi-bot-notifications-toggle";
        location.href = "ixian:disableNotifications";
	}else
    {
        notifications = true;
        el.className = "spixi-switch spixi-bot-notifications-toggle";
        location.href = "ixian:enableNotifications";
	}
}

var clipboardJs = new ClipboardJS('.address_qr_holder');

clipboardJs.on('success', function (e) {
    e.clearSelection();

    var x = document.getElementsByClassName("spixi-toastbar")[0];
    x.className = "spixi-toastbar show";
    setTimeout(function () { x.className = x.className.replace("show", ""); }, 3000);

});

clipboardJs.on('error', function (e) {

});

var botQrCode = new QRCode("BotQrCode", {
    text: "",
    width: 200,
    height: 200,
    colorDark: "#000000",
    colorLight: "#ffffff",
    correctLevel: QRCode.CorrectLevel.H
});

var userQrCode = new QRCode("UserQrCode", {
    text: "",
    width: 200,
    height: 200,
    colorDark: "#000000",
    colorLight: "#ffffff",
    correctLevel: QRCode.CorrectLevel.H
});

function setBotAddress(addr) {
    bot_address = addr;
    var parts = addr.match(/.{1,17}/g) || [];
    document.getElementById("BotWal1").innerHTML = parts[0];
    document.getElementById("BotWal2").innerHTML = parts[1];
    document.getElementById("BotWal3").innerHTML = parts[2];
    document.getElementById("BotWal4").innerHTML = parts[3];
    document.getElementById("BotAddressQrHolder").setAttribute("data-clipboard-text", addr);
    botQrCode.clear(); // clear the code.
    botQrCode.makeCode(addr);
}

function setUserAddress(addr) {
    user_address = addr;
    var parts = addr.match(/.{1,17}/g) || [];
    document.getElementById("UserWal1").innerHTML = parts[0];
    document.getElementById("UserWal2").innerHTML = parts[1];
    document.getElementById("UserWal3").innerHTML = parts[2];
    document.getElementById("UserWal4").innerHTML = parts[3];
    document.getElementById("UserAddressQrHolder").setAttribute("data-clipboard-text", addr);
    userQrCode.clear(); // clear the code.
    userQrCode.makeCode(addr);
}

function toggleSpixiBotAddress(toggleEl, addressElId)
{
    var addressEl = document.getElementById(addressElId);
    if(addressEl.style.display == "none")
    {
        addressEl.style.display = "block";
        toggleEl.className = "fa fa-chevron-up";
	}else
    {
        addressEl.style.display = "none";
        toggleEl.className = "fa fa-chevron-down";
	}
}

function showUserDetails(nick, address)
{
    var userDetailsEl = document.getElementById("UserDetails");
    userDetailsEl.style.display = "block";
    userDetailsEl.getElementsByClassName("spixi-bot-user-nick")[0].innerHTML = nick;
    setUserAddress(address);
}

function hideUserDetails(e)
{
    if(e.target != e.currentTarget)
    {
         return;
	}
    var userDetailsEl = document.getElementById("UserDetails");
    userDetailsEl.style.display = "none";
}

function sendContactRequest(address)
{
    location.href = "ixian:sendContactRequest:" + address;
}

function kickUser()
{
    var address = document.getElementById('UserAddressQrHolder').getAttribute('data-clipboard-text');

    var title = SL_Modals["kickTitle"];
    title = title.replace("{0}", address);

    var html = SL_Modals["kickBody"];
    html = html.replace("{0}", address);

    var payBtnHtml = "<div onclick=\"location.href='ixian:kick:" + address +  "';\">" + SL_Modals["kickButton"] + "</div>";
    var cancelBtnHtml = "<div onclick='hideModalDialog();'>" + SL_Modals["cancel"] + "</div>";

    showModalDialog(title, html, payBtnHtml, cancelBtnHtml);
}

function banUser()
{
    var address = document.getElementById('UserAddressQrHolder').getAttribute('data-clipboard-text');

    var title = SL_Modals["banTitle"];
    title = title.replace("{0}", address);

    var html = SL_Modals["banBody"];
    html = html.replace("{0}", address);

    var payBtnHtml = "<div onclick=\"location.href='ixian:ban:" + address +  "';\">" + SL_Modals["banButton"] + "</div>";
    var cancelBtnHtml = "<div onclick='hideModalDialog();'>" + SL_Modals["cancel"] + "</div>";

    showModalDialog(title, html, payBtnHtml, cancelBtnHtml);
}
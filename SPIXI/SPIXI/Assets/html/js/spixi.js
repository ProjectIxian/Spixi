// SPIXI helper script

function onload()
{
    location.href = "ixian:onload";
    startRelativeTimeUpdate("spixi-rel-ts-active");
}

function isBlank(str) {
    return (!str || /^\s*$/.test(str));
}

function unescapeParameter(str)
{
    return str.replace(/&gt;/g, ">")
            .replace(/&lt;/g, "<")
            .replace(/&#92;/g, "\\")
            .replace(/&#39;/g, "'")
            .replace(/&#34;/g, "\"");
}

function quickScanJS() {
    let scanner = new Instascan.Scanner({});
    scanner.addListener('scan', function (content) {
        location.href = "ixian:qrresult:" + content;
    });
    Instascan.Camera.getCameras().then(function (cameras) {
        if (cameras.length > 0) {
            scanner.start(cameras[0]);
        } else {
            console.error('No cameras found.');
            alert("No cameras found.");
        }
    }).catch(function (e) {
        console.error(e);
        alert("Cannot connect to camera.");
    });
}

function getTimeDifference(unixTimestamp)
{
    var curTime = Date.now();
    var delta = Math.floor(curTime / 1000) - unixTimestamp;
    return delta;
}

function getRelativeTime(unixTimestamp)
{
    var delta = getTimeDifference(unixTimestamp);

    if (delta < 30)
        return "just now";

    if (delta < 90)
        return "a minute ago";

    if (delta < 3600)
        return Math.floor(delta / 60) + " minutes ago";

    var fullDate = new Date(unixTimestamp * 1000);
    return fullDate.toLocaleString();
}

var relativeTimeUpdateinterval = null;

function startRelativeTimeUpdate(className) {
    if (relativeTimeUpdateinterval != null) {
        return;
    }

    relativeTimeUpdateinterval = setInterval(function () {
        try {
            var els = document.getElementsByClassName(className);
            if(els.length == 0)
            {
                clearInterval(relativeTimeUpdateinterval);
                relativeTimeUpdateinterval = null;
			}
            for (var i = 0; i < els.length; i++) {
                var el = els[i];
                var new_ts = getRelativeTime(el.getAttribute("data-timestamp"));
                var match = new_ts.match(/[0-9]+$/);
                if (match == "") {
                    el.innerHTML = new_ts;
                } else {
                    el.innerHTML = new_ts;
                    el.className == el.className.replace(className, "");
                }
            }
        } catch (e) {
        }
    }, 30000);
}

var callTimeUpdateinterval = null;

function startCallTimeUpdate(className)
{
    if (callTimeUpdateinterval != null) {
        return;
    }

    callTimeUpdateinterval = setInterval(function () {
        try {
            var els = document.getElementsByClassName(className);
            for (var i = 0; i < els.length; i++) {
                var el = els[i];
                var totalTime = Math.floor((Date.now() - el.getAttribute("data-start-timestamp")) / 1000);
                var mins = Math.floor(totalTime / 60);
                var secs = Math.floor(totalTime % 60);
                if(secs < 10)
                {
                    secs = "0" + secs;
				}
                el.innerHTML = mins + ":" + secs;
            }
        } catch (e) {
        }
    }, 500);
}

function addAppRequest(sessionId, text, acceptHtml, rejectHtml) {
    removeAppRequest(sessionId);

    var el = document.createElement("div");
    el.id = "AppReq_" + sessionId;
    el.className = "spixi-callbar";

    var acceptAction = "appAccept('" + sessionId + "');";
    var rejectAction = "appReject('" + sessionId + "');";

    acceptHtml = unescapeParameter(acceptHtml);
    rejectHtml = unescapeParameter(rejectHtml);

    el.innerHTML = '<div class="spixi-callbar-title">' + text + '</div><div class="spixi-callbar-separator"></div><div class="row spixi-callbar-actions"><div class="col-6"><div onclick="' + acceptAction + '" style="display:inline-block;">' + acceptHtml + '</div></div><div class="col-6" style="text-align:right;"><div onclick="' + rejectAction + '" style="display:inline-block;">' + rejectHtml + '</div></div></div>';

    document.body.appendChild(el);
}

function removeAppRequest(sessionId) {
    var el = document.getElementById("AppReq_" + sessionId);
    if (el != null) {
        el.parentElement.removeChild(el);
    }
}

function clearAppRequests() {
    var els = document.getElementsByClassName("spixi-callbar");
    for (var i = 0; i < els.length; i++) {
        var el = els[i];
        el.parentElement.removeChild(el);
    }
}

function appAccept(sessionId) {
    var el = document.getElementById("AppReq_" + sessionId);
    el.parentElement.removeChild(el);
    location.href = 'ixian:appAccept:' + sessionId;
}

function appReject(sessionId) {
    var el = document.getElementById("AppReq_" + sessionId);
    el.parentElement.removeChild(el);
    location.href = 'ixian:appReject:' + sessionId;
}

function displayCallBar(sessionId, text, hangUpHtml, displayTime)
{
    var el = document.getElementById("CallBar");
    if(el == null)
    {
        el = document.createElement("div");
        document.body.appendChild(el);
    }else
    {
        el.style.display = "block";
	}
    el.id = "CallBar";
    el.className = "spixi-callbar";

    var rejectAction = "hangUp('" + sessionId + "');";

    hangUpHtml = unescapeParameter(hangUpHtml);

    var timeHtml = "";
    if(displayTime != "0")
    {
        timeHtml = '<div class="spixi-callbar-duration" data-start-timestamp="' + (displayTime * 1000) + '"></div>';
	}

    el.innerHTML = '<div class="spixi-callbar-title">' + text + '</div><div class="spixi-callbar-separator"></div><div class="row spixi-callbar-actions"><div class="col-6">' + timeHtml + '</div><div class="col-6" style="text-align:right;"><div onclick="' + rejectAction + '" style="display:inline-block;">' + hangUpHtml + '</div></div></div>';
    if(displayTime != "0")
    {
        startCallTimeUpdate("spixi-callbar-duration");
    }
}

function hangUp(sessionId)
{
    location.href = 'ixian:hangUp:' + sessionId;
}

function hideCallBar()
{
    document.getElementById("CallBar").style.display = "none";
}

function showWarning(text) {
    var el = document.getElementById("warning_bar");
    if(el == null)
    {
         return;
	}
    if (text == "") {
        el.style.display = 'none';
    }
    else {
        el.style.display = 'block';
        var msgEls = el.getElementsByClassName("spixi-errorbar-message");
        msgEls[0].innerHTML = text;
    }
}

var modalHtml = '<div class="modal-content" onclick="event.stopPropagation(); return false;">\
                <div class="spixi-modal-header warn">\
                </div>\
                <hr class="spixi-separator noheightmargins fullwidth" />\

                <div class="spixi-modal-text">\
                </div>\

                <hr class="spixi-separator noheightmargins fullwidth" />\
                <div class="spixi-modal-footer">\
                    <div class="spixi-modal-button-left"></div>\
                    <div class="spixi-modal-button-right"></div>\
                </div>\
        </div>';

function showModalDialog(title, body, leftButton, rightButton){
    hideModalDialog();

    var modalEl = document.createElement("div");
    modalEl.id = "SpixiModalDialog";
    modalEl.className = "spixi-modal";
    modalEl.innerHTML = modalHtml;
    modalEl.onclick = hideModalDialog;

    modalEl.getElementsByClassName("spixi-modal-header")[0].innerHTML = title;
    modalEl.getElementsByClassName("spixi-modal-text")[0].innerHTML = body;

    modalEl.getElementsByClassName("spixi-modal-button-left")[0].innerHTML = leftButton;
    modalEl.getElementsByClassName("spixi-modal-button-right")[0].innerHTML = rightButton;

    document.body.appendChild(modalEl);
}

function hideModalDialog()
{
    var modalEl = document.getElementById("SpixiModalDialog");
    if(modalEl != null)
    {
        document.body.removeChild(modalEl);
	}
}
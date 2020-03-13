// SPIXI helper script

function onload()
{
    location.href = "ixian:onload";
    startRelativeTimeUpdate("spixi-rel-ts-active");
}

function isBlank(str) {
    return (!str || /^\s*$/.test(str));
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

    if (delta < 15)
        return "just now";

    if (delta < 60)
        return delta + " seconds ago";

    if (delta < 120)
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
            for (var i = 0; i < els.length; i++) {
                var el = els[i];
                var new_ts = getRelativeTime(el.getAttribute("data-timestamp"));
                if (new_ts == el.innerHTML) {
                    el.className == el.className.replace(className, "");
                } else {
                    el.innerHTML = new_ts;
                }
            }
        } catch (e) {
        }
    }, 10000);
}
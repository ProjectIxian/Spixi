﻿var onboardingLocalizedSkip = "SKIP";
var onboardingLocalizedFinish = "FINISH";

function createOnboardingFrame()
{
	var frame = document.createElement("div");
	frame.id = "Onboarding";
	
	var wrap = document.createElement("div");
	wrap.className = "onboarding-wrap";
	frame.appendChild(wrap);

	var imgSection = document.createElement("div");
	imgSection.className = "onboarding-image-section";
	wrap.appendChild(imgSection);

	var textSection = document.createElement("div");
	textSection.className = "onboarding-text-section";
	wrap.appendChild(textSection);

	wrap.innerHTML += "<div class='onboarding-pager'></div>";

	var navSectionWrap = document.createElement("div");
	navSectionWrap.className = "onboarding-nav-section-wrap";
	frame.appendChild(navSectionWrap);

	var navSection = document.createElement("div");
	navSection.className = "onboarding-nav-section";
	navSection.innerHTML = "<div class='onboarding-prev'></div>";
	navSection.innerHTML += "<div class='onboarding-next'></div>";
	navSectionWrap.appendChild(navSection);

	document.body.appendChild(frame);
}

function setOnboardingContents(title, text, section, showPager, showSkip)
{
	var imgSection = document.getElementsByClassName("onboarding-image-section")[0];
	var imgName = "onboarding" + section + ".png";
	imgSection.innerHTML = "";
	if(showSkip)
	{
		imgSection.innerHTML += "<div class='onboarding-skip' onclick='finishOnboarding();'>" + onboardingLocalizedSkip + "<div>";
	}
	imgSection.innerHTML += "<img src='img/xxhdpi/" + imgName + "' srcset='img/ldpi/" + imgName + " 192w,img/mdpi/" + imgName + " 302w,img/hdpi/" + imgName + " 451w,img/xhdpi/" + imgName + " 684w,img/xxhdpi/" + imgName + " 912w' sizes='90vw'/>";

	var textSection = document.getElementsByClassName("onboarding-text-section")[0];
	textSection.innerHTML = "<div class='onboarding-title'>" + title + "</div>";
	textSection.innerHTML += "<div class='onboarding-text'>" + text + "</div>";

	if(showPager)
	{
		var prevSection = document.getElementsByClassName("onboarding-prev")[0];
		if(section > 1)
		{
			prevSection.innerHTML = "<i class='fa fa-chevron-left'></i>";
			prevSection.onclick = function(){ onboarding(section - 1); };
		}else
		{
			prevSection.innerHTML = "";
		}

		var pagerSection = document.getElementsByClassName("onboarding-pager")[0];
		pagerSection.innerHTML = "";
		for(var i = 1; i <= 4; i++)
		{
			var sel = "";
			if(i == section)
			{
				sel = "sel";
			}
			pagerSection.innerHTML += "<i class='fa fa-circle " + sel + "'></i>";
		}

		var nextSection = document.getElementsByClassName("onboarding-next")[0];
		if(section < 4)
		{
			nextSection.innerHTML = "<i class='fa fa-chevron-right'></i>";
			nextSection.onclick = function(){ onboarding( section + 1); };
		}else
		{
			nextSection.innerHTML = onboardingLocalizedFinish;
			nextSection.onclick = finishOnboarding;
		}
	}
}

function finishOnboarding()
{
	var onboardingEl = document.getElementById("Onboarding");
	onboardingEl.parentNode.removeChild(onboardingEl);
	location.href = "ixian:onboardingComplete";
}
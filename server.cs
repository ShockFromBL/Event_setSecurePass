exec("Add-Ons/Event_setSecurePass/Lib_SHA256.cs");

if($Pref::Server::SSP::Feedback $= "")
  $Pref::Server::SSP::Feedback = false;

if($Pref::Server::SSP::UseSalt $= "")
  $Pref::Server::SSP::UseSalt = false;

$SSP::StrList = "A B C D E F G H I J K L M N O P Q R S T U V W X Y Z 0 1 2 3 4 5 6 7 8 9 ! \" £ $ % ^ & * ( ) _ + - = [ ] ; ' # , . / { } : @ ~ < > ? /";

function SSP_generateStr(%len) {
	%str = "";

	for(%i = 0; %i < %len; %i++) {
		%chosen = getWord($SSP::StrList, getRandom(0, getWordCount($SSP::StrList) - 1));

		if(getRandom(0, 1))
			%str = %str @ strUpr(%chosen);
		else
			%str = %str @ strLwr(%chosen);
	}

	return trim(%str);
}

function SSP_generateSalt() {
	if($SSP::Salt $= "") {
    %fo = new FileObject();
		if(isFile(%loc = "config/server/SSP/salt.txt")) {
			echo("[Event_setSecurePass] Using saved salt...");

			%fo.openForRead(%loc);
			$SSP::Salt = %fo.readLine();
		} else {
			warn("[Event_setSecurePass] No salt found, generating...");

			%fo.openForWrite(%loc);
			%fo.writeLine($SSP::Salt = SSP_generateStr(32));
			%fo.writeLine("If the above salt is changed, this file is deleted or this file is moved from its original location, all previous saves which have used setSecurePass w/ the salt will become invalid.");
		}
    %fo.close();
    %fo.delete();
	}

	return $SSP::Salt;
}

if($Pref::Server::SSP::UseSalt)
	SSP_generateSalt();

function SSP_generatePass(%pass) {
	if($Pref::Server::SSP::UseSalt)
		%pass = $SSP::Salt @ %pass;

  return sha256(%pass);
}

function SSP_checkPass(%plaintext, %hash) {
  %sha256 = ($Pref::Server::SSP::UseSalt ? sha256($SSP::Salt @ %plaintext) : sha256(%plaintext));

	if(%sha256 $= %hash)
		return true;

	return false;
}

function serverCmdGenerateSecurePass(%client, %a0, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %fail) {
  if(isEventPending(%client.gspDelay)) {
    messageClient(%client, '', "\c0Please wait before using this command again.");
    return;
  }

	if(%fail !$= "") {
		messageClient(%client, '', "\c0Secure pass generation failed - \c610 word limit\c0 reached!");
		return;
	}

	for(%i = 0; %i < 11; %i++)
		%plaintext = trim(%plaintext SPC %a[%i]);

	%client.gspDelay = %client.schedule(3000, "");

	messageClient(%client, '', "\c5Your hash is\c6: \c3" @ SSP_generatePass(%plaintext));
	messageClient(%client, '', "\c7Enter the above hash into your setSecurePass text box.");
}

function serverCmdSecurePass(%client, %a0, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9) {
	if(!isObject(%player = %client.player))
		return;

	if(%player.securePass["brick"] $= "")
		return;

	for(%i = 0; %i < 10; %i++)
		%plaintext = trim(%plaintext SPC %a[%i]);

  %correct = SSP_checkPass(%plaintext, %player.securePass["brick"].securePass["hash"]);

	if(%correct) {
		if($Pref::Server::SSP::Feedback)
			messageClient(%client, '', "\c2Password accepted.");

		%player.securePass["brick"].onSecurePassCorrect(%client);
	} else {
		if($Pref::Server::SSP::Feedback)
			messageClient(%client, '', "\c0Password failed.");

		%player.securePass["brick"].onSecurePassFail(%client);
	}

	%player.securePass["brick"] = "";
}

function serverCmdSP(%client, %a0, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9) {
	serverCmdSecurePass(%client, %a0, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9);
}

function serverCmdGSP(%client, %a0, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %fail) {
	serverCmdGenerateSecurePass(%client, %a0, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %fail);
}

function FxDTSBrick::setSecurePass(%brick, %hash, %client) {
	if(strLen(%hash) != 64) {
		messageClient(%client, '', "\c0The \c5hash\c0 is invalid, use \c6/GSP \c3[Plaintext Password]\c0 to generate a proper \c5hash\c0.");
    messageClient(%client, '', "<font:Palatino Linotype:20>\c7Alternatively, you can use an <a:http://www.xorbin.com/tools/sha256-hash-calculator>online web service such as this</a>\c7 to copy & paste one to the text box instead.");
		return;
	}

	messageClient(%client, '', "\c7Use \c6/SP \c3[Plaintext Password]\c7 to continue.");

	%player = %client.player;

	%brick.securePass["hash"] = %hash;
	%player.securePass["brick"] = %brick;
}

registerOutputEvent("FxDTSBrick", "setSecurePass", "string 64 300", 1);

function FxDTSBrick::onSecurePassCorrect(%brick, %client) {
	$InputTarget["Self"] = %brick;
	$InputTarget["Client"] = %client;
	$InputTarget["Player"] = %client.player;
	$InputTarget["MiniGame"] = getMiniGameFromObject(%brick);

	%brick.processInputEvent("onSecurePassCorrect", %client);
}

registerInputEvent("FxDTSBrick", "onSecurePassCorrect", "Self FxDTSBrick\tPlayer Player\tClient GameConnection\tMiniGame MiniGame", 1);

function FxDTSBrick::onSecurePassFail(%brick, %client) {
	$InputTarget["Self"] = %brick;
	$InputTarget["Client"] = %client;
	$InputTarget["Player"] = %client.player;
	$InputTarget["MiniGame"] = getMiniGameFromObject(%brick);

	%brick.processInputEvent("onSecurePassFail", %client);
}

registerInputEvent("FxDTSBrick", "onSecurePassFail", "Self FxDTSBrick\tPlayer Player\tClient GameConnection\tMiniGame MiniGame", 1);
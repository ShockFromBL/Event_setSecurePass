$SSP::Pref::Feedback = true;
$SSP::Pref::UseSalt = false;

function SSP_generateStr(%len) {
	%str = "";
	
	%gen = "A B C D E F G H I J K L M N O P Q R S T U V W X Y Z 0 1 2 3 4 5 6 7 8 9 ! \" £ $ % ^ & * ( ) _ + - = [ ] ; ' # , . / { } : @ ~ < > ? /";
	
	for (%i = 0; %i < %len; %i++) {
		%chosen = getWord(%gen, getRandom(0, getWordCount(%gen) - 1));
		
		if (getRandom(0, 1))
			%str = %str @ strUpr(%chosen);
		else
			%str = %str @ strLwr(%chosen);
	}
	
	return trim(%str);
}

function SSP_generateSalt() {
	if ($SSP::Salt $= "") {
		if (isFile(%loc = "config/server/SSP/salt.txt")) {
			echo("Event_setSecurePass - Using saved salt...");
			
			%fo = new FileObject();
			%fo.openForRead(%loc);
			$SSP::Salt = %fo.readLine();
			%fo.close();
			%fo.delete();
		} else {
			warn("Event_setSecurePass - No salt found, generating...");
			
			%fo = new FileObject();
			%fo.openForWrite(%loc);
			%fo.writeLine($SSP::Salt = SSP_generateStr(32));
			%fo.writeLine("If you change the above salt, all previous bricks which have used setSecurePass will become invalid.");
			%fo.close();
			%fo.delete();
		}
	}
	
	return $SSP::Salt;
}

if ($SSP::Pref::UseSalt)
	SSP_generateSalt();

function SSP_generatePass(%pass) {
	if ($SSP::Pref::UseSalt)
		return sha1($SSP::Salt @ %pass);
	else
		return sha1(%pass);
}

function SSP_checkPass(%plaintext, %hash) {
	if ($SSP::Pref::UseSalt)
		%sha = sha1($SSP::Salt @ %plaintext);
	else
		%sha = sha1(%plaintext);
	
	if (%sha $= %hash)
		return true;
	
	return false;
}

function serverCmdGenerateSecurePass(%client, %a0, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %fail) {
	if (%a0 $= "")
		return;
	
	if (%client.gspDelay > $Sim::Time)
		return;
	
	if (%fail !$= "") {
		messageClient(%client, '', "\c0Secure pass generation failed - \c610 word limit\c0 reached!");
		return;
	}
	
	for (%i = 0; %i < 11; %i++)
		%plaintext = trim(%plaintext SPC %a[%i]);
	
	%client.gspDelay = $Sim::Time + 1;
	
	messageClient(%client, '', "\c5Your secure hash is\c6: \c3" @ SSP_generatePass(%plaintext));
	messageClient(%client, '', "\c7Enter the above hash into your setSecurePass text box.");
}

function serverCmdSecurePass(%client, %a0, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9) {
	if (!isObject(%player = %client.player))
		return;
	
	if (%a0 $= "")
		return;
	
	if (%player.securePass["brick"] $= "")
		return;
	
	for (%i = 0; %i < 11; %i++)
		%plaintext = trim(%plaintext SPC %a[%i]);
	
	if (SSP_checkPass(%plaintext, %player.securePass["brick"].securePass["hash"])) {
		if ($SSP::Pref::Feedback)
			messageClient(%client, '', "\c2Password accepted!");
		
		%player.securePass["brick"].onSecurePassCorrect(%client);
	} else {
		if ($SSP::Pref::Feedback)
			messageClient(%client, '', "<color:FF0000>Password denied!");
		
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

registerOutputEvent("FxDTSBrick", "setSecurePass", "string 40 240", 1);

function FxDTSBrick::setSecurePass(%this, %hash, %client) {
	if (strLen(%hash) != 40) {
		messageClient(%client, '', "\c0The \c5secure hash\c0 is invalid, use \c6/GSP \c3[Plaintext Password]\c0 to generate a secure pass.");
		return;
	}
	
	messageClient(%client, '', "\c0This is protected with a \c5secure hash\c0.");
	messageClient(%client, '', "\c7Use \c6/SP \c3[Plaintext Password]\c7 to continue.");
	
	%player = %client.player;
	
	%this.securePass["hash"] = %hash;
	%player.securePass["brick"] = %this;
}

registerInputEvent("FxDTSBrick", "onSecurePassCorrect", "Self FxDTSBrick\tPlayer Player\tClient GameConnection\tMiniGame MiniGame", 1);

function FxDTSBrick::onSecurePassCorrect(%this, %client) {
	$inputTarget_self = %this;
	$inputTarget_client = %client;
	$inputTarget_player = %client.player;
	$inputTarget_miniGame = getMiniGameFromObject(%this);
	
	%this.processInputEvent("onSecurePassCorrect", %client);
}

registerInputEvent("FxDTSBrick", "onSecurePassFail", "Self FxDTSBrick\tPlayer Player\tClient GameConnection\tMiniGame MiniGame", 1);

function FxDTSBrick::onSecurePassFail(%this, %client) {
	$inputTarget_self = %this;
	$inputTarget_client = %client;
	$inputTarget_player = %client.player;
	$inputTarget_miniGame = getMiniGameFromObject(%this);
	
	%this.processInputEvent("onSecurePassFail", %client);
}
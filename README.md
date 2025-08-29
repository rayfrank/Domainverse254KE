# Domainverse254KE
A  mobile app that turns .KE domain search into a walk-around experience. Type a word → domain ideas appear in the city → walk into one to “pick” it → add to cart. 
It also includes a normal application aprat from the gamified version where you can just acquire domains
What you can do

Search
Tap Domain Search, type a keyword (e.g. pizza) and press Search.

Explore
Suggested domains (like pizza.co.ke, getpizza.com) appear as floating text in the scene.

Pick a domain
Move your character into a domain label to collect it.

Cart

Your picks show in Picked Domains.

Tap ADD to cart to move a pick into your Cart.

Use Remove From Cart to take it out.

Checkout shows a simple summary.
Why this is cool

Makes checking available .KE domains feel fun and visual.

Teaches people what a domain is and how it might look for their brand.

Great for events/demos: quick to understand, zero setup.

How to play (Android)

Get the APK (Android app file).

On your phone, allow installs from this source (Settings → Security).

Tap the APK to install, then open the game.

iOS build not included here (requires Apple dev account and Xcode).

Controls

On-screen joystick to move.

Eye button to look around.

Run button to sprint.

Notes on availability

The game checks free/registered status using public domain data (RDAP/WHOIS).

If the network blocks lookups, suggestions still spawn for the demo, but availability may not be exact.

What’s in this repo (in plain English)

Assets/ – Game scenes, scripts, and art.

ProjectSettings/ & Packages/ – Project setup files Unity needs.

(We don’t include giant build folders so the download stays small.)

Credits

KeNIC – .KE domain registry (inspiration & public data).

Unity – game engine.

You – for trying it out!

Contact

Questions or feedback: open an issue on GitHub or email [your email here].

For reviewers (1-minute technical note)

Unity 6 (Android, IL2CPP, API level 34+).

Scripts of interest: DomainSearchAndSpawn.cs, DomainPickup.cs, CartUI.cs.

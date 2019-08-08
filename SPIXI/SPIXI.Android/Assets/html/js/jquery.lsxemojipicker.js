
if(typeof jQuery !== 'undefined'){
    (function ($, win) {
        'use strict';
    
        var emoji = {
            'people': [
                {'name': 'smile', 'value': '&#x1f604'},
                {'name': 'smiley', 'value': '&#x1f603'},
                {'name': 'grinning', 'value': '&#x1f600'},
                {'name': 'blush', 'value': '&#x1f60a'},
                {'name': 'wink', 'value': '&#x1f609'},
                {'name': 'heart-eyes', 'value': '&#x1f60d'},
                {'name': 'kissing-heart', 'value': '&#x1f618'},
                {'name': 'kissing-closed-eyes', 'value': '&#x1f61a'},
                {'name': 'kissing', 'value': '&#x1f617'},
                {'name': 'kissing-smiling-eyes', 'value': '&#x1f619'},
                {'name': 'stuck-out-tongue-winking-eye', 'value': '&#x1f61c'},
                {'name': 'stuck-out-tongue-closed-eyes', 'value': '&#x1f61d'},
                {'name': 'stuck-out-tongue', 'value': '&#x1f61b'},
                {'name': 'flushed', 'value': '&#x1f633'},
                {'name': 'grin', 'value': '&#x1f601'},
                {'name': 'pensive', 'value': '&#x1f614'},
                {'name': 'satisfied', 'value': '&#x1f60c'},
                {'name': 'unamused', 'value': '&#x1f612'},
                {'name': 'disappointed', 'value': '&#x1f61e'},
                {'name': 'persevere', 'value': '&#x1f623'},
                {'name': 'cry', 'value': '&#x1f622'},
                {'name': 'joy', 'value': '&#x1f602'},
                {'name': 'sob', 'value': '&#x1f62d'},
                {'name': 'sleepy', 'value': '&#x1f62a'},
                {'name': 'relieved', 'value': '&#x1f625'},
                {'name': 'cold-sweat', 'value': '&#x1f630'},
                {'name': 'sweat-smile', 'value': '&#x1f605'},
                {'name': 'sweat', 'value': '&#x1f613'},
                {'name': 'weary', 'value': '&#x1f629'},
                {'name': 'tired-face', 'value': '&#x1f62b'},
                {'name': 'fearful', 'value': '&#x1f628'},
                {'name': 'scream', 'value': '&#x1f631'},
                {'name': 'angry', 'value': '&#x1f620'},
                {'name': 'rage', 'value': '&#x1f621'},
                {'name': 'triumph', 'value': '&#x1f624'},
                {'name': 'confounded', 'value': '&#x1f616'},
                {'name': 'laughing', 'value': '&#x1f606'},
                {'name': 'yum', 'value': '&#x1f60b'},
                {'name': 'mask', 'value': '&#x1f637'},
                {'name': 'sunglasses', 'value': '&#x1f60e'},
                {'name': 'sleeping', 'value': '&#x1f634'},
                {'name': 'dizzy-face', 'value': '&#x1f635'},
                {'name': 'astonished', 'value': '&#x1f632'},
                {'name': 'worried', 'value': '&#x1f61f'},
                {'name': 'frowning', 'value': '&#x1f626'},
                {'name': 'anguished', 'value': '&#x1f627'},
                {'name': 'smiling-imp', 'value': '&#x1f608'},
                {'name': 'imp', 'value': '&#x1f47f'},
                {'name': 'open-mouth', 'value': '&#x1f62e'},
                {'name': 'grimacing', 'value': '&#x1f62c'},
                {'name': 'neutral-face', 'value': '&#x1f610'},
                {'name': 'confused', 'value': '&#x1f615'},
                {'name': 'hushed', 'value': '&#x1f62f'},
                {'name': 'no-mouth', 'value': '&#x1f636'},
                {'name': 'innocent', 'value': '&#x1f607'},
                {'name': 'smirk', 'value': '&#x1f60f'},
                {'name': 'expressionless', 'value': '&#x1f611'},
                {'name': 'man-with-gua-pi-mao', 'value': '&#x1f472'},
                {'name': 'man-with-turban', 'value': '&#x1f473'},
                {'name': 'cop', 'value': '&#x1f46e'},
                {'name': 'construction-worker', 'value': '&#x1f477'},
                {'name': 'guardsman', 'value': '&#x1f482'},
                {'name': 'baby', 'value': '&#x1f476'},
                {'name': 'boy', 'value': '&#x1f466'},
                {'name': 'girl', 'value': '&#x1f467'},
                {'name': 'man', 'value': '&#x1f468'},
                {'name': 'woman', 'value': '&#x1f469'},
                {'name': 'older-man', 'value': '&#x1f474'},
                {'name': 'older-woman', 'value': '&#x1f475'},
                {'name': 'person-with-blond-hair', 'value': '&#x1f471'},
                {'name': 'angel', 'value': '&#x1f47c'},
                {'name': 'princess', 'value': '&#x1f478'},
                {'name': 'smiley-cat', 'value': '&#x1f63a'},
                {'name': 'smile-cat', 'value': '&#x1f638'},
                {'name': 'heart-eyes-cat', 'value': '&#x1f63b'},
                {'name': 'kissing-cat', 'value': '&#x1f63d'},
                {'name': 'smirk-cat', 'value': '&#x1f63c'},
                {'name': 'scream-cat', 'value': '&#x1f640'},
                {'name': 'crying-cat-face', 'value': '&#x1f63f'},
                {'name': 'joy-cat', 'value': '&#x1f639'},
                {'name': 'pouting-cat', 'value': '&#x1f63e'},
                {'name': 'japanese-ogre', 'value': '&#x1f479'},
                {'name': 'japanese-goblin', 'value': '&#x1f47a'},
                {'name': 'see-no-evil', 'value': '&#x1f648'},
                {'name': 'hear-no-evil', 'value': '&#x1f649'},
                {'name': 'speak-no-evil', 'value': '&#x1f64a'},
                {'name': 'skull', 'value': '&#x1f480'},
                {'name': 'alien', 'value': '&#x1f47d'},
                {'name': 'poop', 'value': '&#x1f4a9'},
                {'name': 'fire', 'value': '&#x1f525'},
                {'name': 'sparkles', 'value': '&#x2728'},
                {'name': 'star2', 'value': '&#x1f31f'},
                {'name': 'dizzy', 'value': '&#x1f4ab'},
                {'name': 'boom', 'value': '&#x1f4a5'},
                {'name': 'anger', 'value': '&#x1f4a2'},
                {'name': 'sweat-drops', 'value': '&#x1f4a6'},
                {'name': 'droplet', 'value': '&#x1f4a7'},
                {'name': 'zzz', 'value': '&#x1f4a4'},
                {'name': 'dash', 'value': '&#x1f4a8'},
                {'name': 'ear', 'value': '&#x1f442'},
                {'name': 'eyes', 'value': '&#x1f440'},
                {'name': 'nose', 'value': '&#x1f443'},
                {'name': 'tongue', 'value': '&#x1f445'},
                {'name': 'lips', 'value': '&#x1f444'},
                {'name': 'thumbsup', 'value': '&#x1f44d'},
                {'name': 'thumbsdown', 'value': '&#x1f44e'},
                {'name': 'ok-hand', 'value': '&#x1f44c'},
                {'name': 'punch', 'value': '&#x1f44a'},
                {'name': 'fist', 'value': '&#x270a'},
                {'name': 'v', 'value': '&#x270c'},
                {'name': 'wave', 'value': '&#x1f44b'},
                {'name': 'hand', 'value': '&#x270b'},
                {'name': 'open-hands', 'value': '&#x1f450'},
                {'name': 'point-up-2', 'value': '&#x1f446'},
                {'name': 'point-down', 'value': '&#x1f447'},
                {'name': 'point-right', 'value': '&#x1f449'},
                {'name': 'point-left', 'value': '&#x1f448'},
                {'name': 'raised-hands', 'value': '&#x1f64c'},
                {'name': 'pray', 'value': '&#x1f64f'},
                {'name': 'point-up', 'value': '&#x261d'},
                {'name': 'clap', 'value': '&#x1f44f'},
                {'name': 'muscle', 'value': '&#x1f4aa'},
                {'name': 'walking', 'value': '&#x1f6b6'},
                {'name': 'runner', 'value': '&#x1f3c3'},
                {'name': 'dancer', 'value': '&#x1f483'},
                {'name': 'couple', 'value': '&#x1f46b'},
                {'name': 'family', 'value': '&#x1f46a'},
                {'name': 'two-men-holding-hands', 'value': '&#x1f46c'},
                {'name': 'two-women-holding-hands', 'value': '&#x1f46d'},
                {'name': 'couplekiss', 'value': '&#x1f48f'},
                {'name': 'couple-with-heart', 'value': '&#x1f491'},
                {'name': 'dancers', 'value': '&#x1f46f'},
                {'name': 'ok-woman', 'value': '&#x1f646'},
                {'name': 'no-good', 'value': '&#x1f645'},
                {'name': 'information-desk-person', 'value': '&#x1f481'},
                {'name': 'raised-hand', 'value': '&#x1f64b'},
                {'name': 'massage', 'value': '&#x1f486'},
                {'name': 'haircut', 'value': '&#x1f487'},
                {'name': 'nail-care', 'value': '&#x1f485'},
                {'name': 'bride-with-veil', 'value': '&#x1f470'},
                {'name': 'person-with-pouting-face', 'value': '&#x1f64e'},
                {'name': 'person-frowning', 'value': '&#x1f64d'},
                {'name': 'bow', 'value': '&#x1f647'},
                {'name': 'tophat', 'value': '&#x1f3a9'},
                {'name': 'crown', 'value': '&#x1f451'},
                {'name': 'womans-hat', 'value': '&#x1f452'},
                {'name': 'athletic-shoe', 'value': '&#x1f45f'},
                {'name': 'mans-shoe', 'value': '&#x1f45e'},
                {'name': 'sandal', 'value': '&#x1f461'},
                {'name': 'high-heel', 'value': '&#x1f460'},
                {'name': 'boot', 'value': '&#x1f462'},
                {'name': 'shirt', 'value': '&#x1f455'},
                {'name': 'necktie', 'value': '&#x1f454'},
                {'name': 'womans-clothes', 'value': '&#x1f45a'},
                {'name': 'dress', 'value': '&#x1f457'},
                {'name': 'running-shirt-with-sash', 'value': '&#x1f3bd'},
                {'name': 'jeans', 'value': '&#x1f456'},
                {'name': 'kimono', 'value': '&#x1f458'},
                {'name': 'bikini', 'value': '&#x1f459'},
                {'name': 'briefcase', 'value': '&#x1f4bc'},
                {'name': 'handbag', 'value': '&#x1f45c'},
                {'name': 'pouch', 'value': '&#x1f45d'},
                {'name': 'purse', 'value': '&#x1f45b'},
                {'name': 'eyeglasses', 'value': '&#x1f453'},
                {'name': 'ribbon', 'value': '&#x1f380'},
                {'name': 'closed-umbrella', 'value': '&#x1f302'},
                {'name': 'lipstick', 'value': '&#x1f484'},
                {'name': 'yellow-heart', 'value': '&#x1f49b'},
                {'name': 'blue-heart', 'value': '&#x1f499'},
                {'name': 'purple-heart', 'value': '&#x1f49c'},
                {'name': 'green-heart', 'value': '&#x1f49a'},
                {'name': 'heart', 'value': '&#x2764'},
                {'name': 'broken-heart', 'value': '&#x1f494'},
                {'name': 'heartpulse', 'value': '&#x1f497'},
                {'name': 'heartbeat', 'value': '&#x1f493'},
                {'name': 'two-hearts', 'value': '&#x1f495'},
                {'name': 'sparkling-heart', 'value': '&#x1f496'},
                {'name': 'revolving-hearts', 'value': '&#x1f49e'},
                {'name': 'love-letter', 'value': '&#x1f48c'},
                {'name': 'cupid', 'value': '&#x1f498'},
                {'name': 'kiss', 'value': '&#x1f48b'},
                {'name': 'ring', 'value': '&#x1f48d'},
                {'name': 'gem', 'value': '&#x1f48e'},
                {'name': 'bust-in-silhouette', 'value': '&#x1f464'},
                {'name': 'busts-in-silhouette', 'value': '&#x1f465'},
                {'name': 'speech-balloon', 'value': '&#x1f4ac'},
                {'name': 'feet', 'value': '&#x1f463'},
                {'name': 'thought-balloon', 'value': '&#x1f4ad'}
            ],
            'nature': [
                {'name': 'dog', 'value': '&#x1f436'},
                {'name': 'wolf', 'value': '&#x1f43a'},
                {'name': 'cat', 'value': '&#x1f431'},
                {'name': 'mouse', 'value': '&#x1f42d'},
                {'name': 'hamster', 'value': '&#x1f439'},
                {'name': 'rabbit', 'value': '&#x1f430'},
                {'name': 'frog', 'value': '&#x1f438'},
                {'name': 'tiger', 'value': '&#x1f42f'},
                {'name': 'koala', 'value': '&#x1f428'},
                {'name': 'bear', 'value': '&#x1f43b'},
                {'name': 'pig', 'value': '&#x1f437'},
                {'name': 'pig-nose', 'value': '&#x1f43d'},
                {'name': 'cow', 'value': '&#x1f42e'},
                {'name': 'boar', 'value': '&#x1f417'},
                {'name': 'monkey-face', 'value': '&#x1f435'},
                {'name': 'monkey', 'value': '&#x1f412'},
                {'name': 'horse', 'value': '&#x1f434'},
                {'name': 'sheep', 'value': '&#x1f411'},
                {'name': 'elephant', 'value': '&#x1f418'},
                {'name': 'panda-face', 'value': '&#x1f43c'},
                {'name': 'penguin', 'value': '&#x1f427'},
                {'name': 'bird', 'value': '&#x1f426'},
                {'name': 'baby-chick', 'value': '&#x1f424'},
                {'name': 'hatched-chick', 'value': '&#x1f425'},
                {'name': 'hatching-chick', 'value': '&#x1f423'},
                {'name': 'chicken', 'value': '&#x1f414'},
                {'name': 'snake', 'value': '&#x1f40d'},
                {'name': 'turtle', 'value': '&#x1f422'},
                {'name': 'bug', 'value': '&#x1f41b'},
                {'name': 'honeybee', 'value': '&#x1f41d'},
                {'name': 'ant', 'value': '&#x1f41c'},
                {'name': 'beetle', 'value': '&#x1f41e'},
                {'name': 'snail', 'value': '&#x1f40c'},
                {'name': 'octopus', 'value': '&#x1f419'},
                {'name': 'shell', 'value': '&#x1f41a'},
                {'name': 'tropical-fish', 'value': '&#x1f420'},
                {'name': 'fish', 'value': '&#x1f41f'},
                {'name': 'dolphin', 'value': '&#x1f42c'},
                {'name': 'whale', 'value': '&#x1f433'},
                {'name': 'whale2', 'value': '&#x1f40b'},
                {'name': 'cow2', 'value': '&#x1f404'},
                {'name': 'ram', 'value': '&#x1f40f'},
                {'name': 'rat', 'value': '&#x1f400'},
                {'name': 'water-buffalo', 'value': '&#x1f403'},
                {'name': 'tiger2', 'value': '&#x1f405'},
                {'name': 'rabbit2', 'value': '&#x1f407'},
                {'name': 'dragon', 'value': '&#x1f409'},
                {'name': 'racehorse', 'value': '&#x1f40e'},
                {'name': 'goat', 'value': '&#x1f410'},
                {'name': 'rooster', 'value': '&#x1f413'},
                {'name': 'dog2', 'value': '&#x1f415'},
                {'name': 'pig2', 'value': '&#x1f416'},
                {'name': 'mouse2', 'value': '&#x1f401'},
                {'name': 'ox', 'value': '&#x1f402'},
                {'name': 'dragon-face', 'value': '&#x1f432'},
                {'name': 'blowfish', 'value': '&#x1f421'},
                {'name': 'crocodile', 'value': '&#x1f40a'},
                {'name': 'camel', 'value': '&#x1f42b'},
                {'name': 'dromedary-camel', 'value': '&#x1f42a'},
                {'name': 'leopard', 'value': '&#x1f406'},
                {'name': 'cat2', 'value': '&#x1f408'},
                {'name': 'poodle', 'value': '&#x1f429'},
                {'name': 'paw-prints', 'value': '&#x1f43e'},
                {'name': 'bouquet', 'value': '&#x1f490'},
                {'name': 'cherry-blossom', 'value': '&#x1f338'},
                {'name': 'tulip', 'value': '&#x1f337'},
                {'name': 'four-leaf-clover', 'value': '&#x1f340'},
                {'name': 'rose', 'value': '&#x1f339'},
                {'name': 'sunflower', 'value': '&#x1f33b'},
                {'name': 'hibiscus', 'value': '&#x1f33a'},
                {'name': 'maple-leaf', 'value': '&#x1f341'},
                {'name': 'leaves', 'value': '&#x1f343'},
                {'name': 'fallen-leaf', 'value': '&#x1f342'},
                {'name': 'herb', 'value': '&#x1f33f'},
                {'name': 'ear-of-rice', 'value': '&#x1f33e'},
                {'name': 'mushroom', 'value': '&#x1f344'},
                {'name': 'cactus', 'value': '&#x1f335'},
                {'name': 'palm-tree', 'value': '&#x1f334'},
                {'name': 'evergreen-tree', 'value': '&#x1f332'},
                {'name': 'deciduous-tree', 'value': '&#x1f333'},
                {'name': 'chestnut', 'value': '&#x1f330'},
                {'name': 'seedling', 'value': '&#x1f331'},
                {'name': 'blossom', 'value': '&#x1f33c'},
                {'name': 'globe-with-meridians', 'value': '&#x1f310'},
                {'name': 'sun-with-face', 'value': '&#x1f31e'},
                {'name': 'full-moon-with-face', 'value': '&#x1f31d'},
                {'name': 'new-moon-with-face', 'value': '&#x1f31a'},
                {'name': 'new-moon', 'value': '&#x1f311'},
                {'name': 'waxing-crescent-moon', 'value': '&#x1f312'},
                {'name': 'first-quarter-moon', 'value': '&#x1f313'},
                {'name': 'waxing-gibbous-moon', 'value': '&#x1f314'},
                {'name': 'full-moon', 'value': '&#x1f315'},
                {'name': 'waning-gibbous-moon', 'value': '&#x1f316'},
                {'name': 'last-quarter-moon', 'value': '&#x1f317'},
                {'name': 'waning-crescent-moon', 'value': '&#x1f318'},
                {'name': 'last-quarter-moon-with-face', 'value': '&#x1f31c'},
                {'name': 'first-quarter-moon-with-face', 'value': '&#x1f31b'},
                {'name': 'moon', 'value': '&#x1f319'},
                {'name': 'earth-africa', 'value': '&#x1f30d'},
                {'name': 'earth-americas', 'value': '&#x1f30e'},
                {'name': 'earth-asia', 'value': '&#x1f30f'},
                {'name': 'volcano', 'value': '&#x1f30b'},
                {'name': 'milky-way', 'value': '&#x1f30c'},
                {'name': 'shooting-star', 'value': '&#x1f320'},
                {'name': 'star', 'value': '&#x2b50'},
                {'name': 'sunny', 'value': '&#x2600'},
                {'name': 'partly-sunny', 'value': '&#x26c5'},
                {'name': 'cloud', 'value': '&#x2601'},
                {'name': 'zap', 'value': '&#x26a1'},
                {'name': 'umbrella', 'value': '&#x2614'},
                {'name': 'snowflake', 'value': '&#x2744'},
                {'name': 'snowman', 'value': '&#x26c4'},
                {'name': 'cyclone', 'value': '&#x1f300'},
                {'name': 'foggy', 'value': '&#x1f301'},
                {'name': 'rainbow', 'value': '&#x1f308'},
                {'name': 'ocean', 'value': '&#x1f30a'}
            ],
            'object': [
                {'name': 'bamboo', 'value': '&#x1f38d'},
                {'name': 'gift-heart', 'value': '&#x1f49d'},
                {'name': 'dolls', 'value': '&#x1f38e'},
                {'name': 'school-satchel', 'value': '&#x1f392'},
                {'name': 'mortar-board', 'value': '&#x1f393'},
                {'name': 'flags', 'value': '&#x1f38f'},
                {'name': 'fireworks', 'value': '&#x1f386'},
                {'name': 'sparkler', 'value': '&#x1f387'},
                {'name': 'wind-chime', 'value': '&#x1f390'},
                {'name': 'rice-scene', 'value': '&#x1f391'},
                {'name': 'jack-o-lantern', 'value': '&#x1f383'},
                {'name': 'ghost', 'value': '&#x1f47b'},
                {'name': 'santa', 'value': '&#x1f385'},
                {'name': 'christmas-tree', 'value': '&#x1f384'},
                {'name': 'gift', 'value': '&#x1f381'},
                {'name': 'tanabata-tree', 'value': '&#x1f38b'},
                {'name': 'tada', 'value': '&#x1f389'},
                {'name': 'confetti-ball', 'value': '&#x1f38a'},
                {'name': 'balloon', 'value': '&#x1f388'},
                {'name': 'crossed-flags', 'value': '&#x1f38c'},
                {'name': 'crystal-ball', 'value': '&#x1f52e'},
                {'name': 'movie-camera', 'value': '&#x1f3a5'},
                {'name': 'camera', 'value': '&#x1f4f7'},
                {'name': 'video-camera', 'value': '&#x1f4f9'},
                {'name': 'vhs', 'value': '&#x1f4fc'},
                {'name': 'cd', 'value': '&#x1f4bf'},
                {'name': 'dvd', 'value': '&#x1f4c0'},
                {'name': 'minidisc', 'value': '&#x1f4bd'},
                {'name': 'floppy-disk', 'value': '&#x1f4be'},
                {'name': 'computer', 'value': '&#x1f4bb'},
                {'name': 'iphone', 'value': '&#x1f4f1'},
                {'name': 'phone', 'value': '&#x260e'},
                {'name': 'telephone-receiver', 'value': '&#x1f4de'},
                {'name': 'pager', 'value': '&#x1f4df'},
                {'name': 'fax', 'value': '&#x1f4e0'},
                {'name': 'satellite', 'value': '&#x1f4e1'},
                {'name': 'tv', 'value': '&#x1f4fa'},
                {'name': 'radio', 'value': '&#x1f4fb'},
                {'name': 'speaker-waves', 'value': '&#x1f50a'},
                {'name': 'sound', 'value': '&#x1f509'},
                {'name': 'speaker', 'value': '&#x1f508'},
                {'name': 'mute', 'value': '&#x1f507'},
                {'name': 'bell', 'value': '&#x1f514'},
                {'name': 'no-bell', 'value': '&#x1f515'},
                {'name': 'loudspeaker', 'value': '&#x1f4e2'},
                {'name': 'mega', 'value': '&#x1f4e3'},
                {'name': 'hourglass-flowing-sand', 'value': '&#x23f3'},
                {'name': 'hourglass', 'value': '&#x231b'},
                {'name': 'alarm-clock', 'value': '&#x23f0'},
                {'name': 'watch', 'value': '&#x231a'},
                {'name': 'unlock', 'value': '&#x1f513'},
                {'name': 'lock', 'value': '&#x1f512'},
                {'name': 'lock-with-ink-pen', 'value': '&#x1f50f'},
                {'name': 'closed-lock-with-key', 'value': '&#x1f510'},
                {'name': 'key', 'value': '&#x1f511'},
                {'name': 'mag-right', 'value': '&#x1f50e'},
                {'name': 'bulb', 'value': '&#x1f4a1'},
                {'name': 'flashlight', 'value': '&#x1f526'},
                {'name': 'high-brightness', 'value': '&#x1f506'},
                {'name': 'low-brightness', 'value': '&#x1f505'},
                {'name': 'electric-plug', 'value': '&#x1f50c'},
                {'name': 'battery', 'value': '&#x1f50b'},
                {'name': 'mag', 'value': '&#x1f50d'},
                {'name': 'bathtub', 'value': '&#x1f6c1'},
                {'name': 'bath', 'value': '&#x1f6c0'},
                {'name': 'shower', 'value': '&#x1f6bf'},
                {'name': 'toilet', 'value': '&#x1f6bd'},
                {'name': 'wrench', 'value': '&#x1f527'},
                {'name': 'nut-and-bolt', 'value': '&#x1f529'},
                {'name': 'hammer', 'value': '&#x1f528'},
                {'name': 'door', 'value': '&#x1f6aa'},
                {'name': 'smoking', 'value': '&#x1f6ac'},
                {'name': 'bomb', 'value': '&#x1f4a3'},
                {'name': 'gun', 'value': '&#x1f52b'},
                {'name': 'hocho', 'value': '&#x1f52a'},
                {'name': 'pill', 'value': '&#x1f48a'},
                {'name': 'syringe', 'value': '&#x1f489'},
                {'name': 'moneybag', 'value': '&#x1f4b0'},
                {'name': 'yen', 'value': '&#x1f4b4'},
                {'name': 'dollar', 'value': '&#x1f4b5'},
                {'name': 'pound', 'value': '&#x1f4b7'},
                {'name': 'euro', 'value': '&#x1f4b6'},
                {'name': 'credit-card', 'value': '&#x1f4b3'},
                {'name': 'money-with-wings', 'value': '&#x1f4b8'},
                {'name': 'calling', 'value': '&#x1f4f2'},
                {'name': 'e-mail', 'value': '&#x1f4e7'},
                {'name': 'inbox-tray', 'value': '&#x1f4e5'},
                {'name': 'outbox-tray', 'value': '&#x1f4e4'},
                {'name': 'email', 'value': '&#x2709'},
                {'name': 'enveloppe', 'value': '&#x1f4e9'},
                {'name': 'incoming-envelope', 'value': '&#x1f4e8'},
                {'name': 'postal-horn', 'value': '&#x1f4ef'},
                {'name': 'mailbox', 'value': '&#x1f4eb'},
                {'name': 'mailbox-closed', 'value': '&#x1f4ea'},
                {'name': 'mailbox-with-mail', 'value': '&#x1f4ec'},
                {'name': 'mailbox-with-no-mail', 'value': '&#x1f4ed'},
                {'name': 'postbox', 'value': '&#x1f4ee'},
                {'name': 'package', 'value': '&#x1f4e6'},
                {'name': 'memo', 'value': '&#x1f4dd'},
                {'name': 'page-facing-up', 'value': '&#x1f4c4'},
                {'name': 'page-with-curl', 'value': '&#x1f4c3'},
                {'name': 'bookmark-tabs', 'value': '&#x1f4d1'},
                {'name': 'bar-chart', 'value': '&#x1f4ca'},
                {'name': 'chart-with-upwards-trend', 'value': '&#x1f4c8'},
                {'name': 'chart-with-downwards-trend', 'value': '&#x1f4c9'},
                {'name': 'scroll', 'value': '&#x1f4dc'},
                {'name': 'clipboard', 'value': '&#x1f4cb'},
                {'name': 'date', 'value': '&#x1f4c5'},
                {'name': 'calendar', 'value': '&#x1f4c6'},
                {'name': 'card-index', 'value': '&#x1f4c7'},
                {'name': 'file-folder', 'value': '&#x1f4c1'},
                {'name': 'open-file-folder', 'value': '&#x1f4c2'},
                {'name': 'scissors', 'value': '&#x2702'},
                {'name': 'pushpin', 'value': '&#x1f4cc'},
                {'name': 'paperclip', 'value': '&#x1f4ce'},
                {'name': 'black-nib', 'value': '&#x2712'},
                {'name': 'pencil2', 'value': '&#x270f'},
                {'name': 'straight-ruler', 'value': '&#x1f4cf'},
                {'name': 'triangular-ruler', 'value': '&#x1f4d0'},
                {'name': 'closed-book', 'value': '&#x1f4d5'},
                {'name': 'green-book', 'value': '&#x1f4d7'},
                {'name': 'blue-book', 'value': '&#x1f4d8'},
                {'name': 'orange-book', 'value': '&#x1f4d9'},
                {'name': 'notebook', 'value': '&#x1f4d3'},
                {'name': 'notebook-with-decorative-cover', 'value': '&#x1f4d4'},
                {'name': 'ledger', 'value': '&#x1f4d2'},
                {'name': 'books', 'value': '&#x1f4da'},
                {'name': 'open-book', 'value': '&#x1f4d6'},
                {'name': 'bookmark', 'value': '&#x1f516'},
                {'name': 'name-badge', 'value': '&#x1f4db'},
                {'name': 'microscope', 'value': '&#x1f52c'},
                {'name': 'telescope', 'value': '&#x1f52d'},
                {'name': 'newspaper', 'value': '&#x1f4f0'},
                {'name': 'art', 'value': '&#x1f3a8'},
                {'name': 'clapper', 'value': '&#x1f3ac'},
                {'name': 'microphone', 'value': '&#x1f3a4'},
                {'name': 'headphones', 'value': '&#x1f3a7'},
                {'name': 'musical-score', 'value': '&#x1f3bc'},
                {'name': 'musical-note', 'value': '&#x1f3b5'},
                {'name': 'notes', 'value': '&#x1f3b6'},
                {'name': 'musical-keyboard', 'value': '&#x1f3b9'},
                {'name': 'violin', 'value': '&#x1f3bb'},
                {'name': 'trumpet', 'value': '&#x1f3ba'},
                {'name': 'saxophone', 'value': '&#x1f3b7'},
                {'name': 'guitar', 'value': '&#x1f3b8'},
                {'name': 'space-invader', 'value': '&#x1f47e'},
                {'name': 'video-game', 'value': '&#x1f3ae'},
                {'name': 'black-joker', 'value': '&#x1f0cf'},
                {'name': 'flower-playing-cards', 'value': '&#x1f3b4'},
                {'name': 'mahjong', 'value': '&#x1f004'},
                {'name': 'game-die', 'value': '&#x1f3b2'},
                {'name': 'dart', 'value': '&#x1f3af'},
                {'name': 'football', 'value': '&#x1f3c8'},
                {'name': 'basketball', 'value': '&#x1f3c0'},
                {'name': 'soccer', 'value': '&#x26bd'},
                {'name': 'baseball', 'value': '&#x26be'},
                {'name': 'tennis', 'value': '&#x1f3be'},
                {'name': '8ball', 'value': '&#x1f3b1'},
                {'name': 'rugby-football', 'value': '&#x1f3c9'},
                {'name': 'bowling', 'value': '&#x1f3b3'},
                {'name': 'golf', 'value': '&#x26f3'},
                {'name': 'mountain-bicyclist', 'value': '&#x1f6b5'},
                {'name': 'bicyclist', 'value': '&#x1f6b4'},
                {'name': 'checkered-flag', 'value': '&#x1f3c1'},
                {'name': 'horse-racing', 'value': '&#x1f3c7'},
                {'name': 'trophy', 'value': '&#x1f3c6'},
                {'name': 'ski', 'value': '&#x1f3bf'},
                {'name': 'snowboarder', 'value': '&#x1f3c2'},
                {'name': 'swimmer', 'value': '&#x1f3ca'},
                {'name': 'surfer', 'value': '&#x1f3c4'},
                {'name': 'fishing-pole-and-fish', 'value': '&#x1f3a3'},
                {'name': 'coffee', 'value': '&#x2615'},
                {'name': 'tea', 'value': '&#x1f375'},
                {'name': 'sake', 'value': '&#x1f376'},
                {'name': 'baby-bottle', 'value': '&#x1f37c'},
                {'name': 'beer', 'value': '&#x1f37a'},
                {'name': 'beers', 'value': '&#x1f37b'},
                {'name': 'cocktail', 'value': '&#x1f378'},
                {'name': 'tropical-drink', 'value': '&#x1f379'},
                {'name': 'wine-glass', 'value': '&#x1f377'},
                {'name': 'fork-and-knife', 'value': '&#x1f374'},
                {'name': 'pizza', 'value': '&#x1f355'},
                {'name': 'hamburger', 'value': '&#x1f354'},
                {'name': 'fries', 'value': '&#x1f35f'},
                {'name': 'poultry-leg', 'value': '&#x1f357'},
                {'name': 'meat-on-bone', 'value': '&#x1f356'},
                {'name': 'spaghetti', 'value': '&#x1f35d'},
                {'name': 'curry', 'value': '&#x1f35b'},
                {'name': 'fried-shrimp', 'value': '&#x1f364'},
                {'name': 'bento', 'value': '&#x1f371'},
                {'name': 'sushi', 'value': '&#x1f363'},
                {'name': 'fish-cake', 'value': '&#x1f365'},
                {'name': 'rice-ball', 'value': '&#x1f359'},
                {'name': 'rice-cracker', 'value': '&#x1f358'},
                {'name': 'rice', 'value': '&#x1f35a'},
                {'name': 'ramen', 'value': '&#x1f35c'},
                {'name': 'stew', 'value': '&#x1f372'},
                {'name': 'oden', 'value': '&#x1f362'},
                {'name': 'dango', 'value': '&#x1f361'},
                {'name': 'egg', 'value': '&#x1f373'},
                {'name': 'bread', 'value': '&#x1f35e'},
                {'name': 'doughnut', 'value': '&#x1f369'},
                {'name': 'custard', 'value': '&#x1f36e'},
                {'name': 'icecream', 'value': '&#x1f366'},
                {'name': 'ice-cream', 'value': '&#x1f368'},
                {'name': 'shaved-ice', 'value': '&#x1f367'},
                {'name': 'birthday', 'value': '&#x1f382'},
                {'name': 'cake', 'value': '&#x1f370'},
                {'name': 'cookie', 'value': '&#x1f36a'},
                {'name': 'chocolate-bar', 'value': '&#x1f36b'},
                {'name': 'candy', 'value': '&#x1f36c'},
                {'name': 'lollipop', 'value': '&#x1f36d'},
                {'name': 'honey-pot', 'value': '&#x1f36f'},
                {'name': 'apple', 'value': '&#x1f34e'},
                {'name': 'green-apple', 'value': '&#x1f34f'},
                {'name': 'tangerine', 'value': '&#x1f34a'},
                {'name': 'lemon', 'value': '&#x1f34b'},
                {'name': 'cherries', 'value': '&#x1f352'},
                {'name': 'grapes', 'value': '&#x1f347'},
                {'name': 'watermelon', 'value': '&#x1f349'},
                {'name': 'strawberry', 'value': '&#x1f353'},
                {'name': 'peach', 'value': '&#x1f351'},
                {'name': 'melon', 'value': '&#x1f348'},
                {'name': 'banana', 'value': '&#x1f34c'},
                {'name': 'pear', 'value': '&#x1f350'},
                {'name': 'pineapple', 'value': '&#x1f34d'},
                {'name': 'sweet-potato', 'value': '&#x1f360'},
                {'name': 'eggplant', 'value': '&#x1f346'},
                {'name': 'tomato', 'value': '&#x1f345'},
                {'name': 'corn', 'value': '&#x1f33d'}
            ],
            'place': [
                {'name': 'house', 'value': '&#x1f3e0'},
                {'name': 'house-with-garden', 'value': '&#x1f3e1'},
                {'name': 'school', 'value': '&#x1f3eb'},
                {'name': 'office', 'value': '&#x1f3e2'},
                {'name': 'post-office', 'value': '&#x1f3e3'},
                {'name': 'hospital', 'value': '&#x1f3e5'},
                {'name': 'bank', 'value': '&#x1f3e6'},
                {'name': 'convenience-store', 'value': '&#x1f3ea'},
                {'name': 'love-hotel', 'value': '&#x1f3e9'},
                {'name': 'hotel', 'value': '&#x1f3e8'},
                {'name': 'wedding', 'value': '&#x1f492'},
                {'name': 'church', 'value': '&#x26ea'},
                {'name': 'department-store', 'value': '&#x1f3ec'},
                {'name': 'european-post-office', 'value': '&#x1f3e4'},
                {'name': 'private-use', 'value': '&#xe50a'},
                {'name': 'city-sunrise', 'value': '&#x1f307'},
                {'name': 'city-sunset', 'value': '&#x1f306'},
                {'name': 'japanese-castle', 'value': '&#x1f3ef'},
                {'name': 'european-castle', 'value': '&#x1f3f0'},
                {'name': 'tent', 'value': '&#x26fa'},
                {'name': 'factory', 'value': '&#x1f3ed'},
                {'name': 'tokyo-tower', 'value': '&#x1f5fc'},
                {'name': 'japan', 'value': '&#x1f5fe'},
                {'name': 'mount-fuji', 'value': '&#x1f5fb'},
                {'name': 'sunrise-over-mountains', 'value': '&#x1f304'},
                {'name': 'sunrise', 'value': '&#x1f305'},
                {'name': 'stars', 'value': '&#x1f303'},
                {'name': 'statue-of-liberty', 'value': '&#x1f5fd'},
                {'name': 'bridge-at-night', 'value': '&#x1f309'},
                {'name': 'carousel-horse', 'value': '&#x1f3a0'},
                {'name': 'ferris-wheel', 'value': '&#x1f3a1'},
                {'name': 'fountain', 'value': '&#x26f2'},
                {'name': 'roller-coaster', 'value': '&#x1f3a2'},
                {'name': 'ship', 'value': '&#x1f6a2'},
                {'name': 'boat', 'value': '&#x26f5'},
                {'name': 'speedboat', 'value': '&#x1f6a4'},
                {'name': 'rowboat', 'value': '&#x1f6a3'},
                {'name': 'anchor', 'value': '&#x2693'},
                {'name': 'rocket', 'value': '&#x1f680'},
                {'name': 'airplane', 'value': '&#x2708'},
                {'name': 'seat', 'value': '&#x1f4ba'},
                {'name': 'helicopter', 'value': '&#x1f681'},
                {'name': 'steam-locomotive', 'value': '&#x1f682'},
                {'name': 'tram', 'value': '&#x1f68a'},
                {'name': 'station', 'value': '&#x1f689'},
                {'name': 'mountain-railway', 'value': '&#x1f69e'},
                {'name': 'train2', 'value': '&#x1f686'},
                {'name': 'bullettrain-side', 'value': '&#x1f684'},
                {'name': 'bullettrain-front', 'value': '&#x1f685'},
                {'name': 'light-rail', 'value': '&#x1f688'},
                {'name': 'metro', 'value': '&#x1f687'},
                {'name': 'monorail', 'value': '&#x1f69d'},
                {'name': 'tram-car', 'value': '&#x1f68b'},
                {'name': 'railway-car', 'value': '&#x1f683'},
                {'name': 'trolleybus', 'value': '&#x1f68e'},
                {'name': 'bus', 'value': '&#x1f68c'},
                {'name': 'oncoming-bus', 'value': '&#x1f68d'},
                {'name': 'blue-car', 'value': '&#x1f699'},
                {'name': 'oncoming-automobile', 'value': '&#x1f698'},
                {'name': 'car', 'value': '&#x1f697'},
                {'name': 'taxi', 'value': '&#x1f695'},
                {'name': 'oncoming-taxi', 'value': '&#x1f696'},
                {'name': 'articulated-lorry', 'value': '&#x1f69b'},
                {'name': 'truck', 'value': '&#x1f69a'},
                {'name': 'rotating-light', 'value': '&#x1f6a8'},
                {'name': 'police-car', 'value': '&#x1f693'},
                {'name': 'oncoming-police-car', 'value': '&#x1f694'},
                {'name': 'fire-engine', 'value': '&#x1f692'},
                {'name': 'ambulance', 'value': '&#x1f691'},
                {'name': 'minibus', 'value': '&#x1f690'},
                {'name': 'bike', 'value': '&#x1f6b2'},
                {'name': 'aerial-tramway', 'value': '&#x1f6a1'},
                {'name': 'suspension-railway', 'value': '&#x1f69f'},
                {'name': 'mountain-cableway', 'value': '&#x1f6a0'},
                {'name': 'tractor', 'value': '&#x1f69c'},
                {'name': 'barber', 'value': '&#x1f488'},
                {'name': 'busstop', 'value': '&#x1f68f'},
                {'name': 'ticket', 'value': '&#x1f3ab'},
                {'name': 'vertical-traffic-light', 'value': '&#x1f6a6'},
                {'name': 'traffic-light', 'value': '&#x1f6a5'},
                {'name': 'warning', 'value': '&#x26a0'},
                {'name': 'construction', 'value': '&#x1f6a7'},
                {'name': 'beginner', 'value': '&#x1f530'},
                {'name': 'fuelpump', 'value': '&#x26fd'},
                {'name': 'izakaya-lantern', 'value': '&#x1f3ee'},
                {'name': 'slot-machine', 'value': '&#x1f3b0'},
                {'name': 'hotsprings', 'value': '&#x2668'},
                {'name': 'moyai', 'value': '&#x1f5ff'},
                {'name': 'circus-tent', 'value': '&#x1f3aa'},
                {'name': 'performing-arts', 'value': '&#x1f3ad'},
                {'name': 'round-pushpin', 'value': '&#x1f4cd'},
                {'name': 'triangular-flag-on-post', 'value': '&#x1f6a9'},
                {'name': 'cn', 'value': '&#x1f1e8;&#x1f1f3'},
                {'name': 'de', 'value': '&#x1f1e9;&#x1f1ea'},
                {'name': 'es', 'value': '&#x1f1ea;&#x1f1f8'},
                {'name': 'fr', 'value': '&#x1f1eb;&#x1f1f7'},
                {'name': 'gb', 'value': '&#x1f1ec;&#x1f1e7'},
                {'name': 'it', 'value': '&#x1f1ee;&#x1f1f9'},
                {'name': 'jp', 'value': '&#x1f1ef;&#x1f1f5'},
                {'name': 'kr', 'value': '&#x1f1f0;&#x1f1f7'},
                {'name': 'ru', 'value': '&#x1f1f7;&#x1f1fa'},
                {'name': 'us', 'value': '&#x1f1fa;&#x1f1f8'}
            ]
        }
    
        var settings = {};
    
        $.fn.lsxEmojiPicker = function (options) {
    
            // Overriding default options
            settings = $.extend({
                width: 220,
                height: 200,
                twemoji: false,
                closeOnSelect: true,
                onSelect: function(em){}
            }, options);
    
            var appender = $('<div></div>')
                .addClass('lsx-emojipicker-appender');
            var container = $('<div></div>')
                .addClass('lsx-emojipicker-container')
                .css({
                    'top': -(settings.height + 70)
                });
            var wrapper = $('<div></div>')
                .addClass('lsx-emojipicker-wrapper');
    
            var spinnerContainer = $('<div></div>')
                .addClass('spinner-container');
            var spinner = $('<div></div>')
                .addClass('loader');
            spinnerContainer.append(spinner);
    
            var emojiPeopleContainer = $('<div></div>')
                .addClass('lsx-emojipicker-emoji lsx-emoji-tab lsx-emoji-people')
                .css({'width': settings.width, 'height': settings.height});
            var emojiNatureContainer = $('<div></div>')
                .addClass('lsx-emojipicker-emoji lsx-emoji-tab lsx-emoji-nature hidden')
                .css({'width': settings.width, 'height': settings.height});
            var emojiPlaceContainer = $('<div></div>')
                .addClass('lsx-emojipicker-emoji lsx-emoji-tab lsx-emoji-place hidden')
                .css({'width': settings.width, 'height': settings.height});
            var emojiObjectContainer = $('<div></div>')
                .addClass('lsx-emojipicker-emoji lsx-emoji-tab lsx-emoji-object hidden')
                .css({'width': settings.width, 'height': settings.height});
    
            var tabs = $('<ul></ul>')
                .addClass('lsx-emojipicker-tabs');
        
            var peopleEmoji = $('<li></li>')
                .addClass('selected')
                .html(emoji['people'][1].value)
                .click(function(e){
                    e.preventDefault();
                    $('ul.lsx-emojipicker-tabs li').removeClass('selected');
                    $(this).addClass('selected');
                    $('.lsx-emoji-tab').addClass('hidden');
                    emojiPeopleContainer.removeClass('hidden');
                });
            var natureEmoji = $('<li></li>')
                .html(emoji['nature'][0].value)
                .click(function(e){
                    e.preventDefault();
                    $('ul.lsx-emojipicker-tabs li').removeClass('selected');
                    $(this).addClass('selected');
                    $('.lsx-emoji-tab').addClass('hidden');
                    emojiNatureContainer.removeClass('hidden');
                });
            var placeEmoji = $('<li></li>')
                .html(emoji['place'][38].value)
                .click(function(e){
                    e.preventDefault();
                    $('ul.lsx-emojipicker-tabs li').removeClass('selected');
                    $(this).addClass('selected');
                    $('.lsx-emoji-tab').addClass('hidden');
                    emojiPlaceContainer.removeClass('hidden');
                });
            var objectEmoji = $('<li></li>')
                .html(emoji['object'][4].value)
                .click(function(e){
                    e.preventDefault();
                    $('ul.lsx-emojipicker-tabs li').removeClass('selected');
                    $(this).addClass('selected');
                    $('.lsx-emoji-tab').addClass('hidden');
                    emojiObjectContainer.removeClass('hidden');
                });
    
            tabs.append(peopleEmoji)
                .append(natureEmoji)
                .append(placeEmoji)
                .append(objectEmoji);
    
            createEmojiTab('people', emojiPeopleContainer, container);
            createEmojiTab('nature', emojiNatureContainer, container);
            createEmojiTab('place', emojiPlaceContainer, container);
            createEmojiTab('object', emojiObjectContainer, container);
    
            //wrapper.append(spinnerContainer);
            wrapper.append(emojiPeopleContainer)
                   .append(emojiNatureContainer)
                   .append(emojiPlaceContainer)
                   .append(emojiObjectContainer);
            wrapper.append(tabs);
            container.append(wrapper);
            appender.append(container);
            this.append(appender);
    
            
            if(settings.twemoji){
                twemoji.parse(emojiPeopleContainer[0], {size: 72});
                twemoji.parse(emojiNatureContainer[0], {size: 72});
                twemoji.parse(emojiPlaceContainer[0], {size: 72});
                twemoji.parse(emojiObjectContainer[0], {size: 72});
                twemoji.parse(tabs[0], {size: 72});
            }
    
            this.click(function(e){
                e.preventDefault();
                if(!$(e.target).parent().hasClass('lsx-emojipicker-tabs') 
                    && !$(e.target).parent().parent().hasClass('lsx-emojipicker-tabs') 
                    && !$(e.target).parent().hasClass('lsx-emoji-tab')
                    && !$(e.target).parent().parent().hasClass('lsx-emoji-tab')){
                    if(container.is(':visible')){
                        container.hide();
                    } else {
                        container.fadeIn();
                    }
                }
            });
            
            // Apply the plugin to the selected elements
            return this;
        }
    
        function createEmojiTab(type, container, wrapper){
            for(var i = 0; i < emoji[type].length; i++){
                var selectedEmoji = emoji[type][i];
                var emoticon = $('<span></span>')
                    .data('value', selectedEmoji.value)
                    .attr('title', selectedEmoji.name)
                    .html(selectedEmoji.value);
                
                emoticon.click(function(e){
                    e.preventDefault();
                    settings.onSelect({
                        'name': $(this).attr('title'),
                        'value': $(this).data('value')
                    });
                    if(settings.closeOnSelect){
                        wrapper.hide();
                    }
                });
                container.append(emoticon);
            }
        }
    }(jQuery, window));
}

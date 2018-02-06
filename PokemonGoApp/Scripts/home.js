var self = this;
var MAP;
var CURRENT_COORDS = { lat: 31.9056151, lng: -44.21836176 };
var COORDS_TO_ADD = { lat: 0, lng: 0 };
var BASE_URL = "";
var SELECTED_LOCATION_ID = "";
var TIMEOUTS = [];
var LIVE_MODE = false;
var markers = [];
var mapMarkers = [];

var markerExists = function(marker) {
    for(var i = 0; i < markers.length; i++) {
        if(markers[i].Id === marker.Id) {
            return true;
        }
    }
    return false;
}
var clearMap = function() {
    for (var i = 0; i < mapMarkers.length; i++) {
        mapMarkers[i].setMap(null);
    }
    mapMarkers = [];
    markers = [];
}

var addMarker = function (pokemon) {
    markers.push(pokemon);
    var icon = {
        url: BASE_URL + "/Resources/" + pokemon.PokeIndexNum + ".png",
        scaledSize: new google.maps.Size(40, 40)
    };
    var marker = new google.maps.Marker({
        position: new google.maps.LatLng(pokemon.Location.Latitude, pokemon.Location.Longitude),
        icon: icon,
        map: MAP,
        locationId: pokemon.Id,
        times: pokemon.Times,
        pokemonName: pokemon.PokemonName
    });
    mapMarkers.push(marker);
    google.maps.event.addListener(marker, 'click', function () {
        $('#AddInstanceModal').modal({
            backdrop: 'static'
        });
        SELECTED_LOCATION_ID = this.locationId;
        $("#ModalTitle")[0].innerHTML = "Did you see this " + marker.pokemonName + " here too?";
        if (marker.times != null && marker.times.length > 0) {
            $("#Instances").show();
            if (marker.times.length > 1) {
                $("#NumTimes")[0].innerHTML = "<span style='font-weight:bold;'>" + marker.times.length + " others did!</span>";
            }
            else {
                $("#NumTimes")[0].innerHTML = "<span style='font-weight:bold;'>" + marker.times.length + " other did!</span>";
            }
            var innerHtml = "<ul>";
            for (var i = 0; i < marker.times.length; i++) {
                var dateTime = marker.times[i];
                if (dateTime.Minute < 10 && typeof(dateTime.Minute) == "number") {
                    dateTime.Minute = "0" + dateTime.Minute;
                }
                innerHtml += "<li>" + dateTime.DayOfWeek + " at " + dateTime.Hour + ":" + dateTime.Minute + " " + dateTime.TimeOfDay + "</li>";
            }
            innerHtml += "</ul>";
            $("#InstanceList")[0].innerHTML = innerHtml;
        }
    });
}
var updateMap = function(pokeIndex) {
    var bounds = MAP.getBounds();
    var sw = bounds.getSouthWest();
    var ne = bounds.getNorthEast();
    var query = encodeURI("?bottomLeftLongitude=" + sw.lng() + "&bottomLeftLatitude=" + sw.lat() + "&topRightLongitude=" + ne.lng() + "&topRightLatitude=" + ne.lat() + "&zoom=" + MAP.getZoom() + "&pokeIndexNum=" + pokeIndex + "&liveMode=" + LIVE_MODE);
    $.ajax({
        url: "/Home/GetPokemonLocations"+query,
        type: "GET",
        dataType: "json",
        success: function (pokemon) {
            for (var i = 0; i < pokemon.length; i++) {
                if (!markerExists(pokemon[i])) {
                    addMarker(pokemon[i]);
                }
            }
        },
        error: function () {
        }
    });
};

if (!location.origin)
    location.origin = location.protocol + "//" + location.host;

function initMap() {
    MAP = new google.maps.Map(document.getElementById('map'), {
        center: CURRENT_COORDS,
        zoom: 3,
        mapTypeControl: false,
        streetViewControl: false
    });
    MAP.setOptions({ styles: mapStyle });

    // CENTER MAP WITH GEOLOCATION
    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(function(position) {
            var pos = {
                lat: position.coords.latitude,
                lng: position.coords.longitude
            };
            MAP.setCenter(new google.maps.LatLng(pos.lat, pos.lng));
        }, function() {
            // Browser doesn't support Geolocation
        });
    } else {
        // Browser doesn't support Geolocation
    }

    

    MAP.addListener('idle', function () {
        updateMap($("#filterPicker").val());
    });

    MAP.addListener("rightclick", function (event) {
        COORDS_TO_ADD.lat = event.latLng.lat();
        COORDS_TO_ADD.lng = event.latLng.lng();

        $('#AddPokemonModal').modal({
            backdrop: 'static'
        });
    });

    MAP.addListener("mousedown", function (event) {
        var timeout = setTimeout(function () {
            COORDS_TO_ADD.lat = event.latLng.lat();
            COORDS_TO_ADD.lng = event.latLng.lng();

            $('#AddPokemonModal').modal({
                backdrop: 'static'
            });
        }, 1000);
        TIMEOUTS.push(timeout);
    });
    MAP.addListener("mouseup", function (event) {
        for(var i = 0; i < TIMEOUTS.length; i++)
        {
            clearTimeout(TIMEOUTS[i]);
        }
        TIMEOUTS = [];
    });
    MAP.addListener('drag', function () {
        for (var i = 0; i < TIMEOUTS.length; i++) {
            clearTimeout(TIMEOUTS[i]);
        }
        TIMEOUTS = [];
    });

    $("#AddPokemonLocation").click(function () {
        if ($("#addPicker").val() == null || $("#addPicker").val() == "")
        {
            alert("Enter a time first bro!");
            return false;
        }
        $.ajax({
            url: "/Home/AddPokemonLocation",
            type: "POST",
            dataType: "json",
            data: {
                "PokeIndexNum": $("#pokepicker").val(),
                "lat": COORDS_TO_ADD.lat,
                "lng": COORDS_TO_ADD.lng,
                "date":$("#addPicker").val()
            },
            success: function (pokemon) {
                addMarker(pokemon);
                $('#AddPokemonModal').modal('hide');
            },
            error: function () {
            }
        });
    });
    $("#AddPokemonInstance").click(function () {
        if ($("#instancePicker").val() == null || $("#instancePicker").val() == "") {
            alert("Enter a time first bro!");
            return false;
        }
        $.ajax({
            url: "/Home/AddPokemonInstance",
            type: "POST",
            dataType: "json",
            data: {
                "id": SELECTED_LOCATION_ID,
                "date": $("#instancePicker").val()
            },
            success: function (pokemon) {
            },
            error: function () {
            },
            complete: function () {
                $('#AddInstanceModal').modal('hide');
            }
        });
    });

    //shamelessly ripped from SO
    function formatData(data) {
        var $result = $(
            "<span><img class='pokeIcon' src='/Resources/"+data.id+".png'> " + data.text + "</span>"
        );
        return $result;
    };


    $("#pokepicker, #filterPicker").select2({
        width: "100%",
        data: pokedex.sort(function (a, b) {
            if (a.text < b.text) return -1;
            if (a.text > b.text) return 1;
            return 0;
        }),
        templateResult: formatData,
        templateSelection: formatData
    });


    $("#filterPicker").select2({
        data: pokedex.sort(function (a, b) {
            if (a.text < b.text) return -1;
            if (a.text > b.text) return 1;
            return 0;
        }),
        templateResult: formatData,
        templateSelection: formatData
    }).on("change", function (e) {
        clearMap();
        updateMap($("#filterPicker").val());
    });
}
$(".timepicker").datetimepicker();
$(document).ready(function () {
    $('[data-toggle="tooltip"]').tooltip()
    $("#TurnOnLive").click(function () {
        LIVE_MODE = true;
        clearMap();
        $("#TurnOnLive").hide();
        $("#TurnOffLive").show();
        updateMap($("#filterPicker").val());
    });
    $("#TurnOffLive").click(function () {
        LIVE_MODE = false;
        clearMap();
        $("#TurnOnLive").show();
        $("#TurnOffLive").hide();
        updateMap($("#filterPicker").val());
    });
});
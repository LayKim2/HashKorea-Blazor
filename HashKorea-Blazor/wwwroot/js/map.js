// google map

let map;
let searchBox;
let userMarker;

function initMap() {
    initializeMap("AIzaSyBo1iPI7wSqNQkL9f2QsLpmZ_We5n7xs1g", "map", window.locations);
}

function initializeMap(apiKey, elementId, locations) {
    const mapElement = document.getElementById(elementId);

    if (!mapElement) {
        console.error("Map element not found");
        return;
    }

    map = new google.maps.Map(mapElement, {
        center: { lat: 37.5665, lng: 126.9780 }, // Default: Seoul, South Korea
        zoom: 12,
    });

    const infoWindow = new google.maps.InfoWindow();

    locations.forEach(function (location) {

        const marker = new google.maps.Marker({
            position: { lat: location.Lat, lng: location.Lng },
            map: map,
            title: location.Title,
        });

        let testImageUrl = 'https://media.istockphoto.com/id/1676101015/ko/%EC%82%AC%EC%A7%84/%EA%B2%BD%EB%B3%B5%EA%B6%81%EC%9D%80-%EC%84%9D%EC%96%91%EC%9D%B4-%EC%95%84%EB%A6%84%EB%8B%B5%EA%B3%A0-%EC%84%9C%EC%9A%B8-%EB%8C%80%ED%95%9C%EB%AF%BC%EA%B5%AD.jpg?s=612x612&w=0&k=20&c=gKZvvJAShxWls229xvzBJlCHJMJF9rOJn-yOYn1ACeA=';

        const infoWindowContent = `
            <div style="font-family: Arial, sans-serif; color: #333;">
                <div style="text-align: center;">
                    <h3 style="margin: 10px 0; font-size: 18px; color: #0073e6;">${location.Title}</h3>
                    <p style="font-size: 14px; color: #666;">${location.Address}</p>
                    <img src="${testImageUrl}" alt="Image for ${location.Title}" style="width: 100%; max-width: 200px; border-radius: 8px; margin-top: 10px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);">
                </div>
                <div style="margin-top: 15px; text-align: center;">
                    <a href="https://www.google.com/maps?q=${location.Lat},${location.Lng}" target="_blank" style="display: inline-block; margin: 5px; text-decoration: none; background-color: #4285F4; color: white; padding: 8px 15px; border-radius: 5px; font-size: 14px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);">
                        Open in Google Maps
                    </a>
                    <a href="https://map.naver.com/v5/latlng/${location.Lat},${location.Lng}" target="_blank" style="display: inline-block; margin: 5px; text-decoration: none; background-color: #1eae7f; color: white; padding: 8px 15px; border-radius: 5px; font-size: 14px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);">
                        Open in Naver Maps
                    </a>
                </div>
            </div>
        `;

        const infoWindow = new google.maps.InfoWindow({
            content: infoWindowContent,
        });

        // Open the InfoWindow when the marker is clicked
        marker.addListener("click", function () {
            infoWindow.open(map, marker);
        });
    });

    const input = document.getElementById("search-box");
    const options = {
        componentRestrictions: { country: "KR" }, // Restrict search to South Korea
    };
    searchBox = new google.maps.places.SearchBox(input, options);

    map.addListener("bounds_changed", () => {
        searchBox.setBounds(map.getBounds());
    });

    const markers = [];

    searchBox.addListener("places_changed", () => {
        const places = searchBox.getPlaces();

        if (places.length === 0) {
            return;
        }

        markers.forEach((marker) => marker.setMap(null));
        markers.length = 0;

        const bounds = new google.maps.LatLngBounds();
        places.forEach((place) => {
            if (!place.geometry || !place.geometry.location) {
                console.log("Returned place contains no geometry");
                return;
            }

            markers.push(
                new google.maps.Marker({
                    map,
                    title: place.name,
                    position: place.geometry.location,
                })
            );

            if (place.geometry.viewport) {
                bounds.union(place.geometry.viewport);
            } else {
                bounds.extend(place.geometry.location);
            }
        });
        map.fitBounds(bounds);
    });

    const locateBtn = document.getElementById("locate-btn");
    if (locateBtn) {
        locateBtn.addEventListener("click", () => {
            getUserLocation();
        });
    }
}

// This function will load the Google Maps script dynamically
function loadGoogleMapsScript(apiKey, callbackName, locations) {
    const script = document.createElement("script");
    script.src = `https://maps.googleapis.com/maps/api/js?key=${apiKey}&libraries=places&callback=${callbackName}&language=en`;
    script.defer = true;
    script.async = true;

    window.locations = JSON.parse(locations);

    document.head.appendChild(script);
}

// get current user location if they allow
function getUserLocation() {
    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(
            (position) => {
                const userLocation = {
                    lat: position.coords.latitude,
                    lng: position.coords.longitude,
                };

                if (userMarker) {
                    userMarker.setMap(null); // Remove existing marker
                }

                userMarker = new google.maps.Marker({
                    position: userLocation,
                    map: map,
                    title: "Your Location",
                    icon: {
                        url: "http://maps.google.com/mapfiles/ms/icons/blue-dot.png",
                    },
                });

                map.setCenter(userLocation); // Center map to user's location
                map.setZoom(15); // Zoom in on user's location
            },
            (error) => {
                console.error("Error getting location: ", error.message);
                alert("Unable to fetch your location.");
            }
        );
    } else {
        alert("Geolocation is not supported by your browser.");
    }
}


// Initialize the map with multiple markers
window.initializeMapWithMarkers = function (locations) {
    const mapOptions = {
        zoom: 8,
        center: { lat: 37.5665, lng: 126.9780 }, // Default center (Seoul)
    };

    const map = new google.maps.Map(document.getElementById("map"), mapOptions);

    // Loop through each location and add a marker
    locations.forEach(function (location) {
        const marker = new google.maps.Marker({
            position: { lat: location.Lat, lng: location.Lng },
            map: map,
            title: location.Title,
        });
    });
};
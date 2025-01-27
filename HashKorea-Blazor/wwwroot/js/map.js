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

    locations.forEach(async function (location) {

        const defaultIcon = {
            url: '/festival.png',
            scaledSize: new google.maps.Size(32, 32),
            animation: google.maps.Animation.DROP
        };

        const hoverIcon = {
            url: '/festival.png',
            scaledSize: new google.maps.Size(38, 38),
            scaledSize: new google.maps.Size(38, 38),
            transition: 'transform 0.3s ease-in-out'
        };

        const marker = new google.maps.Marker({
            position: { lat: location.Lat, lng: location.Lng },
            map: map,
            title: location.Title,
            icon: defaultIcon
        });

        // hover in
        marker.addListener('mouseover', function () {
            this.setIcon(hoverIcon);
        });

        // hover out
        marker.addListener('mouseout', function () {
            // InfoWindow가 열려있지 않을 때만 기본 크기로 돌아가기
            if (currentInfoWindow !== infoWindow) {
                this.setIcon(defaultIcon);
            }
        });

        let testImageUrl = 'https://media.istockphoto.com/id/1676101015/ko/%EC%82%AC%EC%A7%84/%EA%B2%BD%EB%B3%B5%EA%B6%81%EC%9D%80-%EC%84%9D%EC%96%91%EC%9D%B4-%EC%95%84%EB%A6%84%EB%8B%B5%EA%B3%A0-%EC%84%9C%EC%9A%B8-%EB%8C%80%ED%95%9C%EB%AF%BC%EA%B5%AD.jpg?s=612x612&w=0&k=20&c=gKZvvJAShxWls229xvzBJlCHJMJF9rOJn-yOYn1ACeA=';

        // change address english to korea
        const geocoder = new google.maps.Geocoder();
        const koreanAddress = await new Promise((resolve, reject) => {
            geocoder.geocode({
                address: location.Address,
                language: 'ko'
            }, (results, status) => {
                if (status === 'OK') {
                    resolve(results[0].formatted_address);
                } else {
                    resolve(location.Address); // 변환 실패시 원본 주소 사용
                }
            });
        });

        const infoWindowContent = `
            <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; 
                        padding: 12px; 
                        width: 100%;
                        max-width: 280px;
                        overflow-x: hidden;">
                <div style="display: flex; align-items: center; gap: 12px;">
                    <h2 style="margin: 0; font-size: 16px; color: #202124; flex: 1; 
                               overflow: hidden; text-overflow: ellipsis;">${location.Title}</h2>
                    <div style="display: flex; gap: 4px;">
                        <a href="https://www.google.com/maps/search/${encodeURIComponent(location.Address)}" target="_blank">
                            <img src="https://www.google.com/favicon.ico" alt="Google" style="width: 16px; height: 16px;">
                        </a>
                        <a href="https://map.naver.com/v5/search/${encodeURIComponent(koreanAddress)}" target="_blank">
                            <img src="https://www.naver.com/favicon.ico" alt="Naver" style="width: 16px; height: 16px;">
                        </a>
                    </div>
                </div>
                <p style="margin: 4px 0 8px 0; font-size: 13px; color: #5f6368;">${location.Address}</p>
                <img src="${testImageUrl}" alt="${location.Title}" 
                     style="width: 100%; height: auto; max-height: 140px; object-fit: cover; border-radius: 8px;">
            </div>
        `;

        const infoWindow = new google.maps.InfoWindow({
            content: infoWindowContent,
            disableAutoPan: false,
            pixelOffset: new google.maps.Size(0, -10)
        });

        let currentInfoWindow = null;

        // open popup by pin
        marker.addListener("click", function () {
            if (currentInfoWindow === infoWindow) {
                infoWindow.close();
                currentInfoWindow = null;
                this.setIcon(defaultIcon);  // 팝업창 닫을 때 아이콘 크기 복원
            } else {
                if (currentInfoWindow) {
                    currentInfoWindow.close();
                }
                infoWindow.open(map, marker);
                currentInfoWindow = infoWindow;
                this.setIcon(hoverIcon);  // 팝업창 열 때 아이콘 크기 확대
            }
        });

        infoWindow.addListener('closeclick', function () {
            currentInfoWindow = null;
            marker.setIcon(defaultIcon);  // 마커 크기를 기본 크기로 복원
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
"use strict";

let map;

function getBounds(locations) {
    let bounds = new google.maps.LatLngBounds();
    for (let i = 0; i < locations.length; i++) {
        let position = new google.maps.LatLng(locations[i].coordinate.latitude, locations[i].coordinate.longitude);
        bounds.extend(position);
    }
    
    return bounds;
}

function flagMarkers(locations) {
    const svgMarker = {
        path: "M8 16s6-5.686 6-10A6 6 0 0 0 2 6c0 4.314 6 10 6 10zm0-7a3 3 0 1 1 0-6 3 3 0 0 1 0 6z",
        fillColor: "#d74331",
        fillOpacity: 1,
        scale: 3,
        strokeWeight: 0,
        rotation: 0,
        anchor: new google.maps.Point(8, 15),
    };
    
    let infowindow = new google.maps.InfoWindow();
    let marker;
    
    for (let i = 0; i < locations.length; i++) {
        marker = new google.maps.Marker({
            position: new google.maps.LatLng(locations[i].coordinate.latitude, locations[i].coordinate.longitude),
            map: map,
            icon: svgMarker
        });

        google.maps.event.addListener(marker, 'click', (function(marker, i) {
            return function() {
                infowindow.setContent(getMarkerPopupContent(locations[i]));
                infowindow.open(map, marker);
            }
        })(marker, i));
    }
}

function applyCustomMapTheme() {
    google.maps.event.trigger(map, "resize");
    map.mapTypes.set("styled_map", styledMapType);
    map.setMapTypeId("styled_map");
}

function initMainGoogleMaps(locations) {
    map = new google.maps.Map(document.getElementById("main-map_canvas"), googleMapsOptions);
    
    applyCustomMapTheme();
    flagMarkers(locations);

    let bounds = getBounds(locations);
    map.fitBounds(bounds);

    if (locations.length === 1) {
        setTimeout(function(){
            map.setZoom(googleMapsOptions.zoom);
        }, 1000);
    }
}

function getMarkerPopupContent(location) {
    return ' ' +
        '<div style="text-align: center;">' +
            '<strong style="display: block; margin-bottom: 5px;">' + location.city + '</strong>' +
            '<span style="display: block;">' + location.coordinate.latitude + ', ' + location.coordinate.longitude + '</span>' +
            '<strong style="display: block; margin-top: 10px;">' + location.tokens.length + ' user</strong>' +
        '</div>';
}

function getCityPoints() {
    let points = [];
    if (groupedActiveTokensByCity) {
        let cityList = Object.keys(groupedActiveTokensByCity);
        for (let city of cityList) {
            let activeTokensByCity = groupedActiveTokensByCity[city];
            let coordinate = activeTokensByCity[0].client_info.geo_location.location;
            
            let item = {
                city: city,
                coordinate: {
                    latitude: coordinate.latitude,
                    longitude: coordinate.longitude,
                },
                tokens: activeTokensByCity
            };

            points.push(item);
        }
    }
    
    return points;
}

function convertToUserSession(full) {
    return {
        id: full[0],
        user_id: full[1],
        first_name: full[2],
        last_name: full[3],
        user_name: full[4],
        email_address: full[5],
        created_at: full[6],
        expires_in: full[7],
        expire_time: full[8],
        client_info: full[9],
        is_current_session: full[10]
    };
}

function onSessionItemClick(element) {
    let latitude = element.getAttribute('data-latitude');
    let longitude = element.getAttribute('data-longitude');
    
    let overlayView = new google.maps.OverlayView();
    overlayView.draw = function() {
        
    }
    
    overlayView.setMap(map);
    let projection = overlayView.getProjection();
    if (projection) {
        let current = projection.fromLatLngToContainerPixel(new google.maps.LatLng(latitude, longitude));
        map.setCenter(projection.fromContainerPixelToLatLng({ x:current.x + 300, y:current.y }));
    }
}

function itemTemplate(session) {
    let currentSessionBadge = session.is_current_session ? '<span class="badge badge-success">Current Session</span>' : '';
    let latitude = 0;
    let longitude = 0;
    let city = "";
    let country = "";
    if (session && session.client_info && session.client_info.geo_location && session.client_info.geo_location.location) {
        latitude = session.client_info.geo_location.location.latitude;
        longitude = session.client_info.geo_location.location.longitude;
        city = session.client_info.geo_location.city;
        country = session.client_info.geo_location.country;
    }
    
    let html = `` +
        `<a href="#" class="d-flex flex-stack position-relative bg-hover-light px-8 py-4" data-latitude="` + latitude + `" data-longitude="` + longitude + `" onclick="onSessionItemClick(this)">
            <div class="fw-bold flex-fill">
                <div class="fs-7 mb-1">
                    <i class="bi bi-clock-fill text-primary"></i>
                    <span class="fs-7 text-muted text-uppercase">` + session.created_at + `</span>
                </div>

                <div class="vertical-center-flex">
                    <span class="fs-5 fw-bolder text-dark text-hover-primary me-4" style="line-height: 23px;">` + session.first_name + ` ` + session.last_name + `</span>` + currentSessionBadge + `
                </div>
                <div>
                    <span class="text-gray-500">` + session.user_name + `</span>
                </div>
            </div>
            <div>
                <div><span class="text-gray-500">` + city + `</span></div>
                <div><span class="text-gray-500">` + country + `</span></div>
            </div>
        </a>`;
    
    return html;
}

function initUserSessionsTable() {
    let table = $('#user_sessions_data_table');
    let dataTable = table.DataTable({
        pagingType: 'full_numbers',
        serverSide: true,
        paging: true,
        searching: false,
        processing: true,
        orderMulti: false,
        stateSave: false,
        responsive: false,
        lengthChange: false,
        pageLength: 15,
        displayStart: 0,
        order: [[0, 'asc']],
        searchDelay: 600,
        columnDefs: [
            {
                targets: 0,
                title: '',
                searchable: false,
                orderable: false,
                render: function(data, type, full, meta) {
                    let session = convertToUserSession(full);
                    return itemTemplate(session);
                }
            }
        ],
        ajax: {
            url: '/api/sessions',
            type: 'GET'
        }
    });
}

jQuery(document).ready(function() {
    let locations = getCityPoints();
    initMainGoogleMaps(locations);
    initUserSessionsTable();
});
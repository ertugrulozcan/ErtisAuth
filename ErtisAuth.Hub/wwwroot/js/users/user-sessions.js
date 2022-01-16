"use strict";

function generateUserAgentTableColumn(title, value) {
    return `` +
        `<div class="me-7 mb-3">
            <div class="fw-bold text-muted fs-8">` + title + `</div>
            <div class="fs-8 fw-bolder text-gray-700">
                <span class="w-75px">` + value + `</span>
            </div>
        </div>`;
}

function renderUserAgentTables() {
    $('.raw-user-agent').each(function() {
        let rawUserAgentString = $(this).text();
        let parser = new UAParser();
        parser.setUA(rawUserAgentString);
        let userAgent = parser.getResult();

        let browser = '';
        if (userAgent.browser && userAgent.browser.name) {
            browser = userAgent.browser.name;
            if (userAgent.browser.version) {
                browser += ' (' + userAgent.browser.version + ')';
            }
        }

        let device = '';
        if (userAgent.device && userAgent.device.model) {
            device = userAgent.device.model;
            if (userAgent.device.type && userAgent.device.vendor) {
                device += ' (' + userAgent.device.type + ', ' + userAgent.device.vendor + ')';
            }
            else if (userAgent.device.type) {
                device += ' (' + userAgent.device.type + ')';
            }
        }

        let os = '';
        if (userAgent.os && userAgent.os.name) {
            os = userAgent.os.name;
            if (userAgent.os.version) {
                os += ' (' + userAgent.os.version + ')';
            }
        }

        let engine = '';
        if (userAgent.engine && userAgent.engine.name) {
            engine = userAgent.engine.name;
        }

        let cpu = '';
        if (userAgent.cpu && userAgent.cpu.architecture) {
            cpu = userAgent.cpu.architecture;
        }

        if (browser === '') {
            browser = '-';
        }

        if (device === '') {
            device = '-';
        }

        if (os === '') {
            os = '-';
        }

        if (engine === '') {
            engine = '-';
        }

        if (cpu === '') {
            cpu = '-';
        }

        let html = `<div class="d-flex flex-wrap mt-3" style="border-bottom: 1px solid #cccccc">` +
            generateUserAgentTableColumn('Browser', browser) +
            generateUserAgentTableColumn('Device', device) +
            generateUserAgentTableColumn('OS', os) +
            generateUserAgentTableColumn('Engine', engine) +
            generateUserAgentTableColumn('Architecture', cpu) +
            `</div>`;

        $(this).after(html);
        $(this).remove();
    });
}

function initGoogleMaps(location) {
    let googleMapsOptions = {
        zoom: 8,
        zoomControl: true,
        center: location,
        mapTypeId: google.maps.MapTypeId.ROADMAP
    };

    let map = new google.maps.Map(document.getElementById("map_canvas"), googleMapsOptions);

    let googleMapsMarker = new google.maps.Marker({
        position: location
    });

    googleMapsMarker.setMap(map);

    $("#location-map").css("width", "100%");
    $("#map_canvas").css("width", "100%");

    google.maps.event.trigger(map, "resize");
    map.setCenter(location);
}

jQuery(document).ready(function() {
    renderUserAgentTables();
    
    $('#showOnMapModal').on('show.bs.modal', function (event) {
        let modal = $(this);
        let target = $(event.relatedTarget);

        let latitude = target.attr('data-latitude');
        let longitude = target.attr('data-longitude');

        initGoogleMaps(new google.maps.LatLng(latitude, longitude))
    });
});
"use strict";

let selectedFilter = '*';
let startDate = 0;
let endDate = 0;

function getDefaultDateRange() {
    return {
        start: moment().subtract(29, "days"),
        end: moment()
    };
}

function anyFilterApplied() {
    let appliedEventTypeFilter = selectedFilter && true && selectedFilter !== '' && selectedFilter !== '*';

    let defaultDateRange = getDefaultDateRange();
    let defaultStartDate = defaultDateRange.start;
    let defaultEndDate = defaultDateRange.end;
    let appliedEventTimeStartFilter = startDate && startDate !== 0 && moment(startDate).format("MMMM D, YYYY") !== defaultStartDate.format("MMMM D, YYYY");
    let appliedEventTimeEndFilter = endDate && endDate !== 0 && moment(endDate).format("MMMM D, YYYY") !== defaultEndDate.format("MMMM D, YYYY");

    return appliedEventTypeFilter || appliedEventTimeStartFilter || appliedEventTimeEndFilter;
}

function updateFilterButtonState() {
    let filterButton = $('#filterButton');
    filterButton.removeClass('btn-light');
    filterButton.removeClass('btn-warning');

    if (anyFilterApplied()) {
        filterButton.addClass('btn-warning');
    }
    else {
        filterButton.addClass('btn-light');
    }
}

function resetDatePicker() {
    let eventsFilterDateRangePicker = $("#eventsFilterDateRangePicker");
    let defaultDateRange = getDefaultDateRange();
    eventsFilterDateRangePicker.data('daterangepicker').setStartDate(defaultDateRange.start);
    eventsFilterDateRangePicker.data('daterangepicker').setEndDate(defaultDateRange.end);

    startDate = defaultDateRange.start.valueOf();
    endDate = defaultDateRange.end.valueOf();
}

function initDatePicker() {
    let eventsFilterDateRangePicker = $("#eventsFilterDateRangePicker");

    function datePickerRangeChanged(start, end) {
        eventsFilterDateRangePicker.html(start.format("MMMM D, YYYY") + " - " + end.format("MMMM D, YYYY"));
        startDate = start.valueOf();
        endDate = end.valueOf();
    }

    let defaultDateRange = getDefaultDateRange();
    eventsFilterDateRangePicker.daterangepicker({
        startDate: defaultDateRange.start,
        endDate: defaultDateRange.end,
        ranges: {
            "Today": [moment(), moment()],
            "Yesterday": [moment().subtract(1, "days"), moment().subtract(1, "days")],
            "Last 7 Days": [moment().subtract(6, "days"), moment()],
            "Last 30 Days": [moment().subtract(29, "days"), moment()],
            "This Month": [moment().startOf("month"), moment().endOf("month")],
            "Last Month": [moment().subtract(1, "month").startOf("month"), moment().subtract(1, "month").endOf("month")]
        },
        parentEl: '#eventsFilterMenu',
        element: '#eventsFilterDateRangePicker',
        opens: 'left'
    }, datePickerRangeChanged);

    datePickerRangeChanged(defaultDateRange.start, defaultDateRange.end);
}

function initFiltering() {
    $("#eventsFilterApplyButton").click(function() {
        let eventTypeFilterDropdown = $('#eventTypeFilterDropdown');
        selectedFilter = eventTypeFilterDropdown.val();
        if (!selectedFilter) {
            selectedFilter = '*';
            eventTypeFilterDropdown.val('*').change();
        }

        let table = $('#events_data_table');
        let dataTable = table.DataTable();
        if (dataTable) {
            // console.log('Date range: ' + startDate + ', ' + endDate);
            dataTable.ajax.url('/api/events?event_type=' + selectedFilter + '&' + 'start_date=' + startDate + '&' + 'end_date=' + endDate).load();
        }

        updateFilterButtonState();
    });

    $("#eventsFilterResetButton").click(function() {
        selectedFilter = '*';
        let eventTypeFilterDropdown = $('#eventTypeFilterDropdown');
        eventTypeFilterDropdown.val('*').change();
        resetDatePicker();

        let table = $('#events_data_table');
        let dataTable = table.DataTable();
        if (dataTable) {
            dataTable.ajax.url('/api/events?event_type=' + selectedFilter).load();
        }

        updateFilterButtonState();
    });

    initDatePicker();
}

KTUtil.onDOMContentLoaded((function() {
    initFiltering();
}));
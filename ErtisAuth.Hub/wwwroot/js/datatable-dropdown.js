"use strict";

function getDatatableDropdown(detailUrl, resourceName, itemId, itemName, isDeleteable) {
	if (isDeleteable) {
		let deleteModalAttributes = getDeleteModalButtonAttributes(itemId, resourceName, itemName);
		return `` +
			`<div class="text-end">
				<button id="actionsDropdown_` + itemId + `" type="button" class="btn btn-light btn-active-light-primary btn-sm" data-bs-toggle="dropdown" aria-expanded="false">
					<i class="fas fa-ellipsis-v"></i>
				</button>
				
				<ul class="dropdown-menu menu menu-sub menu-sub-dropdown menu-column menu-rounded menu-gray-600 menu-state-bg-light-primary fw-bold fs-7 py-2 w-150px" aria-labelledby="actionsDropdown_` + itemId + `">
					<li>
						<div class="menu-item px-2">
							<a href="` + detailUrl + `" class="btn btn-active-light-primary btn-sm btn-widest dropdown-button px-4">
								<i class="bi bi-pencil-fill me-3"></i>
								Edit
							</a>
						</div>
					</li>
					<div class="separator my-1"></div>
					<li>
						<div class="menu-item px-2">
							<a href="#" class="btn btn-active-light-primary btn-sm btn-widest dropdown-button px-4" ` + deleteModalAttributes + `>
								<i class="bi bi-trash text-danger me-3"></i>
								Delete
							</a>
						</div>
					</li>
				</ul>                    
			</div>`;
	}
	else {
		return `` +
			`<div class="text-end">
				<button id="actionsDropdown_` + itemId + `" type="button" class="btn btn-light btn-active-light-primary btn-sm" data-bs-toggle="dropdown" aria-expanded="false">
					<i class="fas fa-ellipsis-v"></i>
				</button>
				
				<ul class="dropdown-menu menu menu-sub menu-sub-dropdown menu-column menu-rounded menu-gray-600 menu-state-bg-light-primary fw-bold fs-7 py-2 w-150px" aria-labelledby="actionsDropdown_` + itemId + `">
					<li>
						<div class="menu-item px-2">
							<a href="` + detailUrl + `" class="btn btn-active-light-primary btn-sm btn-widest dropdown-button px-4">
								<i class="bi bi-pencil-fill me-3"></i>
								Details
							</a>
						</div>
					</li>
				</ul>                    
			</div>`;
	}
}
--person
alter table person 
add constraint fk_person_user_id 
	foreign key(userid) 
		references "user"(id) 
		on delete no action on update no action;

--person_characteristic
alter table person_characteristic 
add constraint fk_person_characteristic_person_id 
	foreign key(person_id) 
		references person(id) 
		on delete no action on update no action;

alter table person_characteristic 
add constraint fk_person_characteristic_characteristic_id 
	foreign key(characteristic_id) 
		references characteristic(id) 
		on delete no action on update no action;

--person_inventory
alter table person_inventory 
add constraint fk_person_inventory_person_id 
	foreign key(person_id) 
		references person(id) 
		on delete no action on update no action;

alter table person_inventory 
add constraint fk_person_inventory_inventory_id 
	foreign key(inventory_id) 
		references inventory(id) 
		on delete no action on update no action;

------------------------------------------------------



--release_architect
alter table release_architect 
add constraint fk_release_architect_release_id 
	foreign key(release_id) 
		references release(id) 
		on delete no action on update no action;

--load_history
alter table load_history 
add constraint fk_load_history_client_id 
	foreign key(client_id) 
		references client(id) 
		on delete no action on update no action;

alter table load_history 
add constraint fk_load_history_release_id 
	foreign key(release_id) 
		references release(id) 
		on delete no action on update no action;

alter table load_history 
add constraint fk_load_history_release_architect_id 
	foreign key(architect_id) 
		references release_architect(id) 
		on delete no action on update no action;




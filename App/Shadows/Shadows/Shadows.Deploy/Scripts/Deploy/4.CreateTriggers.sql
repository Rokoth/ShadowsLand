--history_trigger
CREATE OR REPLACE FUNCTION history_trigger()
  RETURNS trigger AS
$BODY$
declare
  user_id                 varchar(70);
  columns_string_to       varchar;
  columns_string_from     varchar;
begin
	
  select current_setting( 'user.id', true ) into user_id;
  
  with a as(
    select 
    array_agg(column_name::varchar) as _columns_string_to,
    array_agg('$2.'||column_name::varchar) as _columns_string_from  
  from 
    information_schema.columns  
  where table_name   = tg_table_name 
    and table_schema = tg_table_schema 
  )  
  select 
    array_to_string(_columns_string_to, ', ') ,
    array_to_string(_columns_string_from, ', ')
  into 
      columns_string_to, columns_string_from
  from a;
  
  execute format( 'insert into %s.h_%s(%s, change_date, user_id) select %s, now(), $1', 
					   tg_table_schema, tg_table_name, columns_string_to, columns_string_from)
		using user_id
		    , ( case when tg_op ilike('%delete%') then old else new end );

  return null;
end;
$BODY$
  LANGUAGE plpgsql VOLATILE
  COST 100;


  CREATE TRIGGER tr_hist_user
  AFTER INSERT OR UPDATE OR DELETE
  ON "user"
  FOR EACH ROW
  EXECUTE PROCEDURE history_trigger();

  CREATE TRIGGER tr_hist_person
  AFTER INSERT OR UPDATE OR DELETE
  ON person
  FOR EACH ROW
  EXECUTE PROCEDURE history_trigger();

  CREATE TRIGGER tr_hist_inventory
  AFTER INSERT OR UPDATE OR DELETE
  ON inventory
  FOR EACH ROW
  EXECUTE PROCEDURE history_trigger();

  CREATE TRIGGER tr_hist_characteristic
  AFTER INSERT OR UPDATE OR DELETE
  ON characteristic
  FOR EACH ROW
  EXECUTE PROCEDURE history_trigger();

  CREATE TRIGGER tr_hist_person_characteristic
  AFTER INSERT OR UPDATE OR DELETE
  ON person_characteristic
  FOR EACH ROW
  EXECUTE PROCEDURE history_trigger();

  CREATE TRIGGER tr_hist_map
  AFTER INSERT OR UPDATE OR DELETE
  ON map
  FOR EACH ROW
  EXECUTE PROCEDURE history_trigger();

  CREATE TRIGGER tr_hist_location
  AFTER INSERT OR UPDATE OR DELETE
  ON "location"
  FOR EACH ROW
  EXECUTE PROCEDURE history_trigger();

  CREATE TRIGGER tr_hist_point
  AFTER INSERT OR UPDATE OR DELETE
  ON point
  FOR EACH ROW
  EXECUTE PROCEDURE history_trigger();

  CREATE TRIGGER tr_hist_ground
  AFTER INSERT OR UPDATE OR DELETE
  ON ground
  FOR EACH ROW
  EXECUTE PROCEDURE history_trigger();

  CREATE TRIGGER tr_hist_nps
  AFTER INSERT OR UPDATE OR DELETE
  ON nps
  FOR EACH ROW
  EXECUTE PROCEDURE history_trigger();

  CREATE TRIGGER tr_hist_nps_characteristic
  AFTER INSERT OR UPDATE OR DELETE
  ON nps_characteristic
  FOR EACH ROW
  EXECUTE PROCEDURE history_trigger();
  
--allow_delete
CREATE OR REPLACE FUNCTION public.before_modify_table()
  RETURNS trigger AS
$BODY$
declare
  allow_delete varchar;  
begin  
  if ( tg_op ilike('%delete%')) then    
    select current_setting( 'allow.delete' ) into allow_delete;      
    if (allow_delete <> 'true') then
      raise exception 'Удаление из таблицы %.% запрещено', tg_table_schema, tg_table_name;
    end if;    
    return old;
  end if;  
  return new;
exception
  when others then 
    raise exception 'Удаление из таблицы %.% запрещено', tg_table_schema, tg_table_name;
end;
$BODY$
  LANGUAGE plpgsql VOLATILE
  COST 100;

CREATE TRIGGER tr_onmodify_user
  AFTER INSERT OR UPDATE OR DELETE
  ON "user"
  FOR EACH ROW
  EXECUTE PROCEDURE before_modify_table();

  CREATE TRIGGER tr_onmodify_person
  AFTER INSERT OR UPDATE OR DELETE
  ON person
  FOR EACH ROW
  EXECUTE PROCEDURE before_modify_table();

  CREATE TRIGGER tr_onmodify_inventory
  AFTER INSERT OR UPDATE OR DELETE
  ON inventory
  FOR EACH ROW
  EXECUTE PROCEDURE before_modify_table();

  CREATE TRIGGER tr_onmodify_characteristic
  AFTER INSERT OR UPDATE OR DELETE
  ON characteristic
  FOR EACH ROW
  EXECUTE PROCEDURE before_modify_table();

  CREATE TRIGGER tr_onmodify_person_characteristic
  AFTER INSERT OR UPDATE OR DELETE
  ON person_characteristic
  FOR EACH ROW
  EXECUTE PROCEDURE before_modify_table();

  CREATE TRIGGER tr_onmodify_map
  AFTER INSERT OR UPDATE OR DELETE
  ON map
  FOR EACH ROW
  EXECUTE PROCEDURE before_modify_table();

  CREATE TRIGGER tr_onmodify_location
  AFTER INSERT OR UPDATE OR DELETE
  ON "location"
  FOR EACH ROW
  EXECUTE PROCEDURE before_modify_table();

  CREATE TRIGGER tr_onmodify_point
  AFTER INSERT OR UPDATE OR DELETE
  ON point
  FOR EACH ROW
  EXECUTE PROCEDURE before_modify_table();

  CREATE TRIGGER tr_onmodify_ground
  AFTER INSERT OR UPDATE OR DELETE
  ON ground
  FOR EACH ROW
  EXECUTE PROCEDURE before_modify_table();

  CREATE TRIGGER tr_onmodify_nps
  AFTER INSERT OR UPDATE OR DELETE
  ON nps
  FOR EACH ROW
  EXECUTE PROCEDURE before_modify_table();
  
  CREATE TRIGGER tr_onmodify_nps_characteristic
  AFTER INSERT OR UPDATE OR DELETE
  ON nps_characteristic
  FOR EACH ROW
  EXECUTE PROCEDURE before_modify_table();